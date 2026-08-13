namespace User.API.Application.Security
{
    // BCrypt en vez de un hasher "simple": genera su propia sal aleatoria por password
    // (no hay tabla de sales que mantener) y su costo es ajustable si el hardware mejora.
    // Es la opción correcta para contraseñas reales, no solo un ejemplo de juguete.
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public bool Verify(string password, string passwordHash) =>
            BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
