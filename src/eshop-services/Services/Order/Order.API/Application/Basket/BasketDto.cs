namespace Order.API.Application.Basket
{
    // Réplica del contrato que expone Basket.API (GET /basket/{userName} -> { cart: {...} }).
    // Se mantiene local para no acoplar Order.API al ensamblado de Basket.API.
    public class BasketDto
    {
        public string UserName { get; set; } = default!;
        public List<BasketItemDto> Items { get; set; } = [];
    }

    public class BasketItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    internal class BasketResponseEnvelope
    {
        public BasketDto Cart { get; set; } = default!;
    }
}
