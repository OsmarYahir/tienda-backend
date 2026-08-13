using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ticket.API.Exceptions
{
    // 401/403 cuando Order.API rechazó el token propagado, 404 cuando la orden no existe,
    // 502 cuando Order.API falló de forma inesperada, 500 genérico sin stack trace para
    // cualquier otra cosa.
    public class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Excepción no controlada en {Path}", context.Request.Path);

            (string Detail, string Title, int StatusCode) details = exception switch
            {
                UnauthorizedException =>
                (
                    exception.Message,
                    nameof(UnauthorizedException),
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized
                ),
                ForbiddenException =>
                (
                    exception.Message,
                    nameof(ForbiddenException),
                    context.Response.StatusCode = StatusCodes.Status403Forbidden
                ),
                NotFoundException =>
                (
                    exception.Message,
                    nameof(NotFoundException),
                    context.Response.StatusCode = StatusCodes.Status404NotFound
                ),
                UpstreamServiceException =>
                (
                    exception.Message,
                    nameof(UpstreamServiceException),
                    context.Response.StatusCode = StatusCodes.Status502BadGateway
                ),
                BadHttpRequestException =>
                (
                    "El cuerpo o los parámetros de la petición no son válidos.",
                    "BadRequestException",
                    context.Response.StatusCode = StatusCodes.Status400BadRequest
                ),
                _ =>
                (
                    "Ha ocurrido un error inesperado. Intente nuevamente más tarde.",
                    "InternalServerError",
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError
                )
            };

            var problemDetails = new ProblemDetails
            {
                Title = details.Title,
                Detail = details.Detail,
                Status = details.StatusCode,
                Instance = context.Request.Path
            };

            problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

            await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
