using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Order.API.Exceptions
{
    // Middleware de errores global: 400 para reglas de negocio/validación incumplidas,
    // 404 para recursos inexistentes, 500 para todo lo demás SIN exponer stack trace
    // ni el mensaje interno de la excepción no controlada (solo se loguea en servidor).
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
                NotFoundException =>
                (
                    exception.Message,
                    nameof(NotFoundException),
                    context.Response.StatusCode = StatusCodes.Status404NotFound
                ),
                // El binding automático del body (p. ej. un enum de OrderStatus con un valor
                // de texto que no existe, o JSON mal formado) falla ANTES de llegar al
                // endpoint/validador, y ASP.NET Core lo envuelve en BadHttpRequestException.
                // Sin este caso, caía al "_" de abajo y devolvía 500 por una petición inválida.
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
