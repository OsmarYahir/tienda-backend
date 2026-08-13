using Order.API.Application.Contracts;
using Order.API.Domain;

namespace Order.API.Application
{
    public interface IOrderService
    {
        // IsNew=false cuando la petición fue una repetición de una Idempotency-Key ya procesada.
        Task<(Domain.Order Order, bool IsNew)> CreateOrderAsync(
            CreateOrderRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);

        Task<Domain.Order> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Domain.Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

        // Listado general — protegido con RequireRole("Admin") en el endpoint, no aquí:
        // el servicio de aplicación no debería conocer de roles/HTTP.
        Task<IReadOnlyList<Domain.Order>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Domain.Order> UpdateStatusAsync(string id, OrderStatus newStatus, CancellationToken cancellationToken = default);
    }
}
