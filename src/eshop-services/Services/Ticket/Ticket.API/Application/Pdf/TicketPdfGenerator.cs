using QuestPDF.Fluent;
using Ticket.API.Application.Orders;

namespace Ticket.API.Application.Pdf
{
    public class TicketPdfGenerator : ITicketPdfGenerator
    {
        public byte[] Generate(OrderDto order, string customerDisplayName) =>
            new TicketDocument(order, customerDisplayName).GeneratePdf();
    }
}
