using System.Net;
using System.Net.Http.Json;

namespace Ticket.API.Application.Users
{
    // El Authorization header saliente lo agrega AuthorizationPropagationHandler (mismo
    // handler que usa OrderApiClient, registrado en este HttpClient también).
    // A diferencia de OrderApiClient, aquí un fallo NO se traduce en una excepción: es
    // "mejor esfuerzo" para enriquecer el ticket con el email del cliente, no un requisito
    // para poder generarlo.
    public class UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger) : IUserApiClient
    {
        public async Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync($"/api/users/{userId}", cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "No fue posible contactar a User.API para resolver el cliente {UserId}", userId);
                return null;
            }

            using (response)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogWarning(
                        "User.API respondió {StatusCode} al consultar el usuario {UserId}; el ticket mostrará el Id sin resolver.",
                        response.StatusCode, userId);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
            }
        }
    }
}
