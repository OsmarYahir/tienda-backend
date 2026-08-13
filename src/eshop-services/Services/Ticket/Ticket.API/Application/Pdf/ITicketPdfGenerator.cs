using Ticket.API.Application.Orders;

namespace Ticket.API.Application.Pdf
{
    public interface ITicketPdfGenerator
    {
        byte[] Generate(OrderDto order, string customerDisplayName);
    }
}
