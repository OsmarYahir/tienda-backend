using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Order.API.Application;
using Order.API.Application.Basket;
using Order.API.Application.Contracts;
using Order.API.Application.Security;
using Order.API.Endpoints;
using Order.API.Exceptions;
using Order.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) MongoDB Atlas: la cadena de conexión NUNCA se hardcodea. Se lee de
//    configuración, la cual en runtime se resuelve con variables de entorno:
//      MongoDb__ConnectionString = mongodb+srv://usuario:password@cluster.mongodb.net
//      MongoDb__DatabaseName     = OrdersDb
//      MongoDb__OrdersCollectionName = orders   (opcional, tiene default)
// ---------------------------------------------------------------------------
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.AddSingleton<OrdersDbContext>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ---------------------------------------------------------------------------
// 2) Cliente HTTP hacia Basket.API (para obtener el carrito al crear la orden)
// ---------------------------------------------------------------------------
// appsettings.json trae "BasketApi:BaseUrl" = "" (a propósito, para no quemar una URL de
// entorno ahí). Un simple "??" NO cubre ese caso: solo reemplaza null, y una cadena vacía
// desde appsettings.json no es null, así que el fallback nunca se aplicaba y `new Uri("")`
// tumbaba el servicio con UriFormatException en cuanto se resolvía IBasketClient.
var basketApiBaseUrlConfig = builder.Configuration["BasketApi:BaseUrl"];
var basketApiBaseUrl = string.IsNullOrWhiteSpace(basketApiBaseUrlConfig)
    ? "http://localhost:5122"
    : basketApiBaseUrlConfig;
builder.Services.AddHttpClient<IBasketClient, BasketClient>(client =>
{
    client.BaseAddress = new Uri(basketApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ---------------------------------------------------------------------------
// 3) Autenticación/Autorización (RBAC): Order.API no emite tokens, solo valida
//    los que emite User.API — por eso necesita la MISMA clave/issuer/audience
//    (Jwt__SecretKey, Jwt__Issuer, Jwt__Audience) inyectadas como variables de
//    entorno en ambos servicios.
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    throw new InvalidOperationException(
        "JWT no está configurado. Define las variables de entorno 'Jwt__SecretKey', 'Jwt__Issuer' y 'Jwt__Audience' " +
        "(deben coincidir con las de User.API, quien firma los tokens).");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Por defecto, JwtSecurityTokenHandler REMAPEA claims cortos conocidos (incluido
        // "role") a las URIs largas de ClaimTypes de .NET al validar el token — así que
        // RoleClaimType = "role" apuntaría a un claim que ya no existe con ese nombre
        // después del mapeo, y RequireRole("Admin") siempre daría 403 aunque el token
        // sea válido y el rol sea correcto. Se desactiva ese remapeo para que los claims
        // queden EXACTAMENTE como los firmó User.API ("sub", "email", "role").
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "email"
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// 4) Validación (FluentValidation)
// ---------------------------------------------------------------------------
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// Los enums viajan como texto ("Pending"/"Confirmed"/"Cancelled") en el JSON,
// más legibles para un frontend en React que un índice numérico.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ---------------------------------------------------------------------------
// 5) CORS: habilita que el frontend React consuma la API, incluyendo el envío
//    del header custom "Idempotency-Key" y del header "Authorization" (si no
//    se declaran explícitamente, el navegador los bloquea en el preflight).
// ---------------------------------------------------------------------------
const string CorsPolicyName = "ReactClient";
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins is { Length: > 0 })
            policy.WithOrigins(allowedOrigins);
        else
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173"); // CRA / Vite

        policy
            .AllowAnyMethod()
            .WithHeaders("Content-Type", "Idempotency-Key", "Authorization")
            .WithExposedHeaders("Location");
    });
});

// ---------------------------------------------------------------------------
// 6) Manejo global de errores: 400 (validación/reglas de negocio), 401/403
//    (autenticación/autorización), 404, y 500 genérico que jamás expone stack
//    trace ni detalles internos (ver CustomExceptionHandler).
// ---------------------------------------------------------------------------
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------------------------------------------------------------------------
// 7) Swagger / OpenAPI, con soporte para inyectar el Bearer token y probar
//    los endpoints protegidos directamente desde la UI.
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Order.API",
        Version = "v1",
        Description = "Microservicio de Órdenes de Compra — MongoDB Atlas + Minimal API + JWT/RBAC"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Pega el token emitido por User.API (POST /api/auth/login). Solo el valor, sin el prefijo 'Bearer '."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order.API v1");
    });
}

app.UseCors(CorsPolicyName);
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOrderEndpoints();
app.MapHealthChecks("/health");

app.Run();
