namespace Ticket.API.Application.Users
{
    // Réplica del contrato público de User.API (GET /api/users/{id}).
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
