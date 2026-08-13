using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Ticket.API.Application.Orders
{
    // DelegatingHandler registrado en el HttpClient tipado hacia Order.API: antes de que
    // cada petición saliente se envíe, copia el header Authorization de la petición HTTP
    // ENTRANTE (la que el cliente le hizo a Ticket.API) hacia la petición saliente.
    //
    // Esto es lo que pide el requerimiento explícitamente: "extraer el JWT de la petición
    // entrante y propagarlo hacia Order.API". No se guarda el token en ningún lado, no se
    // valida aquí (Order.API sigue siendo la autoridad) — solo se reenvía tal cual llegó.
    public class AuthorizationPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var incomingAuthHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrWhiteSpace(incomingAuthHeader) &&
                AuthenticationHeaderValue.TryParse(incomingAuthHeader, out var authHeader))
            {
                request.Headers.Authorization = authHeader;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
