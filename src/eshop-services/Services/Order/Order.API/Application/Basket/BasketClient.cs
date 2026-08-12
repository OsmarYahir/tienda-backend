using System.Net;
using System.Net.Http.Json;
using Order.API.Exceptions;

namespace Order.API.Application.Basket
{
    // Cliente HTTP tipado hacia Basket.API (GET /basket/{basketId}).
    // BaseAddress se configura vía appsettings/env var "BasketApi:BaseUrl" (Program.cs).
    public class BasketClient(HttpClient httpClient, ILogger<BasketClient> logger) : IBasketClient
    {
        public async Task<BasketDto?> GetBasketAsync(string basketId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync($"/basket/{basketId}", cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "No fue posible contactar a Basket.API para el carrito {BasketId}", basketId);
                throw new BadRequestException("El servicio de carrito no está disponible en este momento.");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Basket.API respondió {StatusCode} al consultar el carrito {BasketId}",
                        response.StatusCode, basketId);
                    throw new BadRequestException("No fue posible consultar el carrito de compras.");
                }

                var envelope = await response.Content.ReadFromJsonAsync<BasketResponseEnvelope>(cancellationToken: cancellationToken);
                return envelope?.Cart;
            }
        }
    }
}
