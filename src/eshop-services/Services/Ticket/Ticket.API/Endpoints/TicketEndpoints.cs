using Ticket.API.Application.Orders;
using Ticket.API.Application.Pdf;

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
            ITicketPdfGenerator pdfGenerator,
            CancellationToken cancellationToken)
        {
            var order = await orderApiClient.GetOrderByIdAsync(id, cancellationToken);
            var pdfBytes = pdfGenerator.Generate(order);

            return Results.File(pdfBytes, "application/pdf", $"ticket-{order.Id}.pdf");
        }
    }
}
