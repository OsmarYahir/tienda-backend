namespace Order.API.Infrastructure
{
    // Se llena por binding de configuración (appsettings + variables de entorno).
    // El ConnectionString real de MongoDB Atlas NUNCA vive en el repositorio:
    // se inyecta en runtime vía la variable de entorno MongoDb__ConnectionString.
    public class MongoDbSettings
    {
        public const string SectionName = "MongoDb";

        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string OrdersCollectionName { get; set; } = "orders";
    }
}
