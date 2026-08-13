namespace Ticket.API.Application.Orders
{
    public interface IOrderApiClient
    {
        Task<OrderDto> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default);
    }
}
