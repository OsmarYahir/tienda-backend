using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Order.API.Infrastructure
{
    // Envuelve la colección de Mongo y garantiza, en el arranque, los índices
    // necesarios: búsqueda rápida por CustomerId y unicidad de Idempotency-Key.
    public class OrdersDbContext
    {
        public IMongoCollection<Domain.Order> Orders { get; }

        public OrdersDbContext(IOptions<MongoDbSettings> settings)
        {
            var config = settings.Value;

            if (string.IsNullOrWhiteSpace(config.ConnectionString))
                throw new InvalidOperationException(
                    "MongoDB no está configurado. Define la variable de entorno 'MongoDb__ConnectionString' " +
                    "(cadena de conexión de MongoDB Atlas) antes de iniciar Order.API.");

            if (string.IsNullOrWhiteSpace(config.DatabaseName))
                throw new InvalidOperationException(
                    "MongoDB no está configurado. Define la variable de entorno 'MongoDb__DatabaseName'.");

            var client = new MongoClient(config.ConnectionString);
            var database = client.GetDatabase(config.DatabaseName);
            Orders = database.GetCollection<Domain.Order>(config.OrdersCollectionName);

            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            var customerIndex = new CreateIndexModel<Domain.Order>(
                Builders<Domain.Order>.IndexKeys.Ascending(o => o.CustomerId));

            // Índice único PARCIAL: solo aplica a documentos donde IdempotencyKey existe,
            // así las órdenes creadas sin ese header (no obligatorio) no colisionan entre sí.
            var idempotencyIndex = new CreateIndexModel<Domain.Order>(
                Builders<Domain.Order>.IndexKeys.Ascending(o => o.IdempotencyKey),
                new CreateIndexOptions<Domain.Order>
                {
                    Unique = true,
                    PartialFilterExpression = Builders<Domain.Order>.Filter.Exists(o => o.IdempotencyKey)
                });

            Orders.Indexes.CreateMany([customerIndex, idempotencyIndex]);
        }
    }
}
