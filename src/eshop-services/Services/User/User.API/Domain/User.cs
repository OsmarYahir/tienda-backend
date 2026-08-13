namespace User.API.Domain
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;

        // "Role" se modela como string (tal como pide el requerimiento), pero solo se
        // construye a través de Register(), que siempre asigna un valor de UserRoles.
        public string Role { get; set; } = UserRoles.Customer;

        public static User Register(string email, string passwordHash)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                // Normalizado a minúsculas: evita duplicados tipo "a@b.com" vs "A@B.com"
                // y hace que la búsqueda por email en login/registro sea determinista.
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                Role = UserRoles.Customer
            };
        }
    }
}
