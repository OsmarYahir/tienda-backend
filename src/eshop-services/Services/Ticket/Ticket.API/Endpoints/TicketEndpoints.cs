using Ticket.API.Application.Orders;
using Ticket.API.Application.Pdf;
using Ticket.API.Application.Users;

namespace Ticket.API.Endpoints
{
    public static class TicketEndpoints
    {
        public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/tickets").WithTags("Tickets");

            group.MapGet("/order/{id}", GetTicketForOrder)
                .WithName("GetTicketForOrder")
                .WithSummary("Genera el PDF del ticket de una orden")
                .WithDescription("Consulta la orden en Order.API (propagando el header Authorization de esta " +
                                  "petición) y devuelve un PDF listo para descargar/imprimir.")
                .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status502BadGateway);

            return app;
        }

        private static async Task<IResult> GetTicketForOrder(
            string id,
            IOrderApiClient orderApiClient,
            IUserApiClient userApiClient,
            ITicketPdfGenerator pdfGenerator,
            CancellationToken cancellationToken)
        {
            var order = await orderApiClient.GetOrderByIdAsync(id, cancellationToken);

            // Mejor esfuerzo: si User.API no responde o el usuario ya no existe, el ticket
            // se genera igual mostrando el Id como respaldo, en vez de fallar la descarga
            // completa por un problema de un servicio secundario.
            var customer = await userApiClient.GetUserByIdAsync(order.CustomerId, cancellationToken);
            var customerDisplayName = customer?.Email ?? order.CustomerId;

            var pdfBytes = pdfGenerator.Generate(order, customerDisplayName);

            return Results.File(pdfBytes, "application/pdf", $"ticket-{order.Id}.pdf");
        }
    }
}
