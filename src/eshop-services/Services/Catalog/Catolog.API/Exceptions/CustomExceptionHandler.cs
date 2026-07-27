using Microsoft.AspNetCore.Diagnostics;

namespace Catalog.API.Exeptions
{
    public class CustomExceptionHandler : IExceptionHandler 
    {
        private readonly ILogger <CustomExceptionHandler> _logger;

        public CustomExceptionHandler(
            ILogger <CustomExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Excepcion capturada");

            var statusCode = StatusCodes.Status500InternalServerError;

            if (exception is ValidationException)
            {
                statusCode = StatusCodes.Status400BadRequest;
            }

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    Title = exception.GetType().Name,
                    Status = statusCode,
                    Detail = exception.Message
                },
                cancellationToken);

            return true;
        }
    }
}
