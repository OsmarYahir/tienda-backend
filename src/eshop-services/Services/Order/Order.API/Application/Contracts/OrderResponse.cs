using Order.API.Domain;

namespace Order.API.Application.Contracts
{
    public record OrderItemResponse(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    public record OrderResponse(
        string Id,
        string CustomerId,
        DateTime CreatedAt,
        OrderStatus Status,
        IReadOnlyList<OrderItemResponse> Items,
        decimal Subtotal,
        decimal Tax,
        decimal Total)
    {
        // Se usa "Domain.Order" (calificado relativo al namespace Order.API) en vez de "Order"
        // a secas: el tipo se llama igual que el primer segmento del namespace raíz
        // (Order.API), y eso puede generar ambigüedad de resolución en C#.
        public static OrderResponse FromDomain(Domain.Order order) => new(
            order.Id,
            order.CustomerId,
            order.CreatedAt,
            order.Status,
            order.Items
                .Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal))
                .ToList(),
            order.Subtotal,
            order.Tax,
            order.Total);
    }
}
