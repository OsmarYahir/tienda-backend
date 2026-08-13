namespace User.API.Application.Security
{
    // Se llena por binding de configuración (appsettings + variables de entorno).
    // La clave real de firma NUNCA vive en el repositorio: se inyecta en runtime vía
    // la variable de entorno Jwt__SecretKey (igual que MongoDb__ConnectionString en Order.API).
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; } = 60;
    }
}
