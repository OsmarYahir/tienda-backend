using System.Net;
using System.Net.Http.Json;
using Ticket.API.Exceptions;

namespace Ticket.API.Application.Orders
{
    // El Authorization header saliente lo agrega AuthorizationPropagationHandler
    // (registrado como message handler de este HttpClient) — aquí solo se arma la
    // petición y se traduce el status code de la respuesta a las excepciones de dominio
    // que CustomExceptionHandler ya sabe convertir en la respuesta HTTP correcta.
    public class OrderApiClient(HttpClient httpClient, ILogger<OrderApiClient> logger) : IOrderApiClient
    {
        public async Task<OrderDto> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync($"/api/orders/{orderId}", cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "No fue posible contactar a Order.API para la orden {OrderId}", orderId);
                throw new UpstreamServiceException("El servicio de órdenes no está disponible en este momento.");
            }

            using (response)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var order = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: cancellationToken);
                        return order ?? throw new UpstreamServiceException("Order.API devolvió una respuesta vacía.");

                    case HttpStatusCode.NotFound:
                        throw new NotFoundException("Order", orderId);

                    case HttpStatusCode.Unauthorized:
                        throw new UnauthorizedException("Se requiere un token válido para generar el ticket de esta orden.");

                    case HttpStatusCode.Forbidden:
                        throw new ForbiddenException("No tienes permisos para generar el ticket de esta orden.");

                    default:
                        logger.LogError("Order.API respondió {StatusCode} al consultar la orden {OrderId}",
                            response.StatusCode, orderId);
                        throw new UpstreamServiceException("No fue posible obtener la información de la orden.");
                }
            }
        }
    }
}
