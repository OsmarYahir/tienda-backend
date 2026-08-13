namespace Ticket.API.Application.Orders
{
    // Réplica del contrato público de Order.API (GET /api/orders/{id}).
    // Se mantiene local, igual que BasketDto en Order.API, para no acoplar Ticket.API
    // al ensamblado de otro microservicio.
    public class OrderDto
    {
        public string Id { get; set; } = default!;
        public string CustomerId { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = default!;
        public List<OrderItemDto> Items { get; set; } = [];
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
