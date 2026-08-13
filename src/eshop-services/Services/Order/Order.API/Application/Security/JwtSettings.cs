namespace Order.API.Application.Security
{
    // MISMOS nombres de variable de entorno que User.API (Jwt__SecretKey, Jwt__Issuer,
    // Jwt__Audience): Order.API no emite tokens, solo los valida, pero para validar la
    // firma correctamente necesita la MISMA clave/issuer/audience con la que User.API
    // los firmó. Es responsabilidad del despliegue inyectar el mismo valor de
    // Jwt__SecretKey en ambos servicios.
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
    }
}
