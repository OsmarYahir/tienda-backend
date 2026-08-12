using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Order.API.Exceptions;

namespace Order.API.Domain
{
    public class Order
    {
        // Único mapa de transiciones válidas: Pending -> {Confirmed, Cancelled}.
        // Confirmed y Cancelled son estados terminales (Cancelled nunca vuelve a Confirmed).
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
            [OrderStatus.Confirmed] = [],
            [OrderStatus.Cancelled] = []
        };

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string CustomerId { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonRepresentation(BsonType.String)]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public List<OrderItem> Items { get; set; } = [];

        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        // Clave de idempotencia opcional. Índice único parcial en Mongo (ver OrdersDbContext)
        // para que dos peticiones concurrentes con la misma clave no generen dos órdenes.
        public string? IdempotencyKey { get; set; }

        public static Order Create(string customerId, List<OrderItem> items, decimal taxRate, string? idempotencyKey)
        {
            if (items is null || items.Count == 0)
                throw new BadRequestException("No se puede crear la orden: el carrito está vacío.");

            var subtotal = Math.Round(items.Sum(i => i.LineTotal), 2);
            var tax = Math.Round(subtotal * taxRate, 2);

            return new Order
            {
                CustomerId = customerId,
                Items = items,
                Subtotal = subtotal,
                Tax = tax,
                Total = subtotal + tax,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = idempotencyKey
            };
        }

        // Encapsula la regla de negocio de transición de estados dentro de la entidad
        // (en vez de dejarla dispersa en el servicio o en el endpoint).
        public void ChangeStatus(OrderStatus newStatus)
        {
            if (newStatus == Status)
                return; // Mismo estado: operación idempotente, no es un error.

            if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            {
                throw new BadRequestException(
                    $"Transición de estado inválida: no se puede pasar de '{Status}' a '{newStatus}'.");
            }

            Status = newStatus;
        }
    }
}
