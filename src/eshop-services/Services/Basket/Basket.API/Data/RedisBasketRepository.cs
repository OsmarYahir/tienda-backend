using System.Text.Json;
using Basket.API.Exeptions;
using Microsoft.Extensions.Caching.Distributed;

namespace Basket.API.Data
{
    public class RedisBasketRepository(IDistributedCache cache) : IBasketRepository
    {
        public async Task<ShoppingCart> GetBasket(string userName, CancellationToken cancellationToken = default)
        {
            var basket = await cache.GetStringAsync(userName, cancellationToken);
            if (string.IsNullOrEmpty(basket))
                throw new BasketNotFoundException(userName);

            return JsonSerializer.Deserialize<ShoppingCart>(basket)!;
        }

        public async Task<ShoppingCart> StoreBasket(ShoppingCart basket, CancellationToken cancellationToken = default)
        {
            await cache.SetStringAsync(basket.UserName, JsonSerializer.Serialize(basket), cancellationToken);
            return basket;
        }

        public async Task DeleteBasket(string userName, CancellationToken cancellationToken = default)
        {
            await cache.RemoveAsync(userName, cancellationToken);
        }
    }
}
