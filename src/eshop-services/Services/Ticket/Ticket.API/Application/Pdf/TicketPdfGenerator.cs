using QuestPDF.Fluent;
using Ticket.API.Application.Orders;

namespace Ticket.API.Application.Pdf
{
    public class TicketPdfGenerator : ITicketPdfGenerator
    {
        public byte[] Generate(OrderDto order) => new TicketDocument(order).GeneratePdf();
    }
}
