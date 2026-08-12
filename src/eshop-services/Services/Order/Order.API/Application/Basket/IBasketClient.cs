namespace Order.API.Application.Basket
{
    public interface IBasketClient
    {
        // Devuelve null si el carrito no existe (Basket.API respondió 404).
        Task<BasketDto?> GetBasketAsync(string basketId, CancellationToken cancellationToken = default);
    }
}
