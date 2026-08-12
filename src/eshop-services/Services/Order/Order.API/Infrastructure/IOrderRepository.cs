namespace Order.API.Infrastructure
{
    public interface IOrderRepository
    {
        Task<Domain.Order> CreateAsync(Domain.Order order, CancellationToken cancellationToken = default);

        Task<Domain.Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Domain.Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

        Task<Domain.Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        Task UpdateAsync(Domain.Order order, CancellationToken cancellationToken = default);
    }
}
