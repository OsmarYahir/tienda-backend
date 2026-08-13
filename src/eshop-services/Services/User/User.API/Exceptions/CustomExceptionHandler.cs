using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace User.API.Exceptions
{
    // Middleware de errores global: 400 (validación/reglas de negocio), 401 (credenciales
    // inválidas), 409 (conflicto, ej. email duplicado), 500 genérico que jamás expone
    // stack trace ni el mensaje interno de una excepción no controlada.
    public class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Excepción no controlada en {Path}", context.Request.Path);

            (string Detail, string Title, int StatusCode) details = exception switch
            {
                ValidationException validationEx =>
                (
                    string.Join(" ", validationEx.Errors.Select(e => e.ErrorMessage)),
                    "ValidationException",
                    context.Response.StatusCode = StatusCodes.Status400BadRequest
                ),
                BadRequestException =>
                (
                    exception.Message,
                    nameof(BadRequestException),
                    context.Response.StatusCode = StatusCodes.Status400BadRequest
                ),
                UnauthorizedException =>
                (
                    exception.Message,
                    nameof(UnauthorizedException),
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized
                ),
                ConflictException =>
                (
                    exception.Message,
                    nameof(ConflictException),
                    context.Response.StatusCode = StatusCodes.Status409Conflict
                ),
                // El binding automático del body (JSON mal formado, un campo que no calza
                // con el tipo esperado) falla antes de llegar al endpoint/validador y ASP.NET
                // Core lo envuelve en BadHttpRequestException — sin este caso, una petición
                // mal formada del cliente devolvería 500 en vez de 400 (bug real que ya
                // encontramos en producción en Order.API).
                BadHttpRequestException =>
                (
                    "El cuerpo de la petición no es válido. Verifique el formato y los valores enviados.",
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

            if (exception is ValidationException validationException)
            {
                problemDetails.Extensions.Add("validationErrors", validationException.Errors);
            }

            await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
