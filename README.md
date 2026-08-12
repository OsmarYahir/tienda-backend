# tienda-backend — Microservicios .NET

Backend de una tienda en línea construido como un conjunto de microservicios independientes,
cada uno con su propia base de datos y ciclo de despliegue. Pensado para ser consumido por un
frontend en React.

## Servicios

| Servicio | Framework | Base de datos | Responsabilidad | Puerto local (docker-compose) |
|---|---|---|---|---|
| **Catalog.API** | ASP.NET Core 9 Minimal API + Marten | PostgreSQL | CRUD de productos, paginado, búsqueda por categoría | `8080` |
| **Basket.API** | ASP.NET Core 9 Minimal API | Redis | Carrito de compras por usuario (guardar / consultar / eliminar) | `18081`* |
| **Order.API** | ASP.NET Core 8 Minimal API | MongoDB Atlas | Órdenes de compra: crea la orden a partir de un carrito, idempotencia, transiciones de estado | `8082` |

\* En Windows el puerto `8081` suele estar reservado por el sistema operativo (Hyper-V/WSL), por
eso el docker-compose local expone Basket.API en `18081`. En Render no aplica esta restricción.

Los tres comparten un patrón común: **Minimal API + FluentValidation + CORS + manejo global de
errores vía `IExceptionHandler`** (`400` para reglas de negocio/validación, `404` para recursos
inexistentes, `500` genérico sin exponer stack trace). Catalog.API y Basket.API comparten esa
infraestructura desde el proyecto `BuildingBlocks`; Order.API, al ser `net8.0` (BuildingBlocks es
`net9.0` y no puede referenciarse entre frameworks distintos), replica el mismo patrón de forma
autónoma para no acoplar su ciclo de release al resto.

## Arquitectura y flujo de una compra

```
React (frontend)
   │
   ├── GET  /products                     → Catalog.API  (Postgres)
   ├── POST /basket                       → Basket.API   (Redis)
   │
   └── POST /api/orders  { customerId, basketId }
             │  header: Idempotency-Key
             ▼
         Order.API ── GET /basket/{basketId} ──▶ Basket.API
             │
             ├─ recalcula Subtotal / Tax / Total en el servidor
             └─ persiste en MongoDB Atlas
```

Order.API **nunca confía en precios enviados desde el cliente**: al crear una orden, consulta el
carrito real en Basket.API y recalcula los totales.

## Order.API — detalle

### Dominio

- `Order`: `Id`, `CustomerId`, `CreatedAt`, `Status` (`Pending` | `Confirmed` | `Cancelled`),
  `Items`, `Subtotal`, `Tax`, `Total`, `IdempotencyKey`.
- `OrderItem`: `ProductId`, `ProductName`, `Quantity`, `UnitPrice`, `LineTotal`.
- Transiciones de estado válidas (encapsuladas en `Order.ChangeStatus`):
  `Pending → Confirmed`, `Pending → Cancelled`. `Confirmed` y `Cancelled` son estados
  terminales — una orden `Cancelled` **no puede** volver a `Confirmed`.

### Idempotencia

El header `Idempotency-Key` es opcional. Si se envía:

1. Antes de crear la orden, se busca si esa clave ya fue usada. Si existe, se devuelve la orden
   ya creada con `200 OK` (no se duplica).
2. Si no existe, se crea la orden con `201 Created`.
3. MongoDB tiene un **índice único parcial** sobre `IdempotencyKey` (solo aplica a documentos
   donde el campo existe, ya que el header no es obligatorio). Esto cubre la condición de
   carrera de dos peticiones concurrentes con la misma clave: si ambas intentan insertar, Mongo
   rechaza la segunda y el servicio recupera y devuelve la orden que sí quedó persistida.

### Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/orders` | Body: `{ customerId, basketId }`. Header opcional: `Idempotency-Key`. `201` si crea, `200` si repite una clave ya procesada, `400` si el carrito está vacío/no existe. |
| `GET` | `/api/orders/{id}` | `200` con la orden, `404` si no existe. |
| `GET` | `/api/orders/customer/{customerId}` | Lista las órdenes de un cliente (`200`, arreglo vacío si no tiene). |
| `PATCH` | `/api/orders/{id}/status` | Body: `{ status: "Confirmed" \| "Cancelled" \| "Pending" }`. Valida la transición; `400` si es inválida. |

Swagger disponible en `/swagger` cuando `ASPNETCORE_ENVIRONMENT=Development`.

## Correr todo localmente (Docker Compose)

```bash
cd src/eshop-services
export MONGODB_CONNECTION_STRING="<tu connection string de MongoDB Atlas>"
export MONGODB_DATABASE_NAME="OrdersDb"
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d --build
```

- Catalog.API → http://localhost:8080
- Basket.API → http://localhost:18081
- Order.API → http://localhost:8082 (Swagger: http://localhost:8082/swagger)

## Variables de entorno

Ningún secreto está escrito en el código ni en los `appsettings.json` versionados. Todo se
inyecta por variable de entorno.

### Order.API

| Variable | Ejemplo | Notas |
|---|---|---|
| `MongoDb__ConnectionString` | `mongodb+srv://usuario:password@cluster.mongodb.net/?appName=Cluster0` | **Secreto.** Cadena de conexión de MongoDB Atlas. |
| `MongoDb__DatabaseName` | `OrdersDb` | Nombre de la base de datos dentro del clúster. |
| `BasketApi__BaseUrl` | `https://tienda-backend-pw7c.onrender.com` | URL pública de Basket.API en Render. |
| `Cors__AllowedOrigins__0` | `https://tu-frontend.vercel.app` | Origen del frontend React en producción. |
| `Orders__TaxRate` | `0.16` | Opcional, ya trae ese valor por default. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Desactiva Swagger y usa `appsettings.json` (no el `.Development.json`). |

### Basket.API

| Variable | Notas |
|---|---|
| `ConnectionStrings__Redis` | Cadena de conexión a la instancia de Redis. |
| `Cors__AllowedOrigins__0` | Origen del frontend React en producción. |

### Catalog.API

| Variable | Notas |
|---|---|
| `ConnectionStrings__CatalogDb` | Cadena de conexión a PostgreSQL. |
| `Cors__AllowedOrigins__0` | Origen del frontend React en producción. |

## Despliegue en Render

Cada microservicio es un **Web Service** de Render independiente, construido desde su propio
`Dockerfile`:

| Servicio Render | Root/Dockerfile | Estado |
|---|---|---|
| `catalog-api-grtz` | `src/eshop-services/Services/Catalog/Catolog.API/Dockerfile` | ✅ desplegado |
| `tienda-backend-pw7c` | `src/eshop-services/Services/Basket/Basket.API/Dockerfile` | ✅ desplegado |
| *(pendiente de crear)* | `src/eshop-services/Services/Order/Order.API/Dockerfile` | ⬜ crear como nuevo Web Service |

Para Order.API: nuevo Web Service → tipo *Docker* → apuntar al Dockerfile de arriba (contexto de
build en la raíz del repo) → agregar las variables de la tabla de Order.API en la sección
*Environment* (marcando `MongoDb__ConnectionString` como *Secret*).
