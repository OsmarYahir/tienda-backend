using MongoDB.Driver;
using Order.API.Application.Basket;
using Order.API.Application.Contracts;
using Order.API.Domain;
using Order.API.Exceptions;
using Order.API.Infrastructure;

namespace Order.API.Application
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IBasketClient _basketClient;
        private readonly ILogger<OrderService> _logger;
        private readonly decimal _taxRate;

        public OrderService(
            IOrderRepository repository,
            IBasketClient basketClient,
            ILogger<OrderService> logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _basketClient = basketClient;
            _logger = logger;
            _taxRate = configuration.GetValue<decimal?>("Orders:TaxRate") ?? 0.16m;
        }

        public async Task<(Domain.Order Order, bool IsNew)> CreateOrderAsync(
            CreateOrderRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
        {
            // 1) Idempotencia: si ya se procesó esta clave, se devuelve la orden existente
            //    en vez de crear un duplicado.
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
                if (existing is not null)
                {
                    _logger.LogInformation(
                        "Idempotency-Key {Key} ya fue procesada. Devolviendo orden {OrderId} existente sin duplicar.",
                        idempotencyKey, existing.Id);
                    return (existing, false);
                }
            }

            // 2) Se "simula" la llamada al Basket obteniendo el carrito real vía HTTP.
            var basket = await _basketClient.GetBasketAsync(request.BasketId, cancellationToken);
            if (basket is null || basket.Items.Count == 0)
                throw new BadRequestException("No se puede crear la orden: el carrito está vacío o no existe.");

            // 3) Se recalculan los totales en el servidor (nunca se confía en precios del cliente).
            var items = basket.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.Price,
                LineTotal = Math.Round(i.Quantity * i.Price, 2)
            }).ToList();

            var order = Domain.Order.Create(request.CustomerId, items, _taxRate, idempotencyKey);

            try
            {
                await _repository.CreateAsync(order, cancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Carrera entre dos peticiones concurrentes con la misma Idempotency-Key:
                // el índice único de Mongo rechazó el insert porque la otra petición ganó.
                // Se devuelve la orden que sí quedó persistida, en vez de fallar.
                var existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey!, cancellationToken);
                if (existing is not null)
                    return (existing, false);

                throw;
            }

            return (order, true);
        }

        public async Task<Domain.Order> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var order = await _repository.GetByIdAsync(id, cancellationToken);
            return order ?? throw new NotFoundException(nameof(Domain.Order), id);
        }

        public Task<IReadOnlyList<Domain.Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            return _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        }

        public Task<IReadOnlyList<Domain.Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _repository.GetAllAsync(cancellationToken);
        }

        public async Task<Domain.Order> UpdateStatusAsync(string id, OrderStatus newStatus, CancellationToken cancellationToken = default)
        {
            var order = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(nameof(Domain.Order), id);

            order.ChangeStatus(newStatus); // Valida la transición; lanza BadRequestException si es inválida.

            await _repository.UpdateAsync(order, cancellationToken);
            return order;
        }
    }
}
