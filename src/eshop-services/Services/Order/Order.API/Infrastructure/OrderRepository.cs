using MongoDB.Bson;
using MongoDB.Driver;

namespace Order.API.Infrastructure
{
    public class OrderRepository(OrdersDbContext context) : IOrderRepository
    {
        private readonly IMongoCollection<Domain.Order> _orders = context.Orders;

        public async Task<Domain.Order> CreateAsync(Domain.Order order, CancellationToken cancellationToken = default)
        {
            await _orders.InsertOneAsync(order, cancellationToken: cancellationToken);
            return order;
        }

        public Task<Domain.Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Un Id con formato inválido rompería el filtro de Mongo (FormatException) antes
            // de siquiera consultar; se trata como "no encontrado" en vez de dejarlo burbujear
            // como un 500.
            if (!ObjectId.TryParse(id, out _))
                return Task.FromResult<Domain.Order?>(null);

            return _orders.Find(o => o.Id == id).FirstOrDefaultAsync(cancellationToken)!;
        }

        public async Task<IReadOnlyList<Domain.Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            return await _orders.Find(o => o.CustomerId == customerId)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public Task<Domain.Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            return _orders.Find(o => o.IdempotencyKey == idempotencyKey).FirstOrDefaultAsync(cancellationToken)!;
        }

        public async Task UpdateAsync(Domain.Order order, CancellationToken cancellationToken = default)
        {
            await _orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: cancellationToken);
        }
    }
}
