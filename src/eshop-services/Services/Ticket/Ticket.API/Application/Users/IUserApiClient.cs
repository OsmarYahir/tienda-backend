namespace Ticket.API.Application.Users
{
    public interface IUserApiClient
    {
        /// <summary>
        /// Devuelve null (nunca lanza) si no se pudo resolver el usuario: el ticket sigue
        /// siendo válido sin el nombre "bonito" del cliente, así que esto no debe tumbar
        /// la generación del PDF completo por un problema de un servicio secundario.
        /// </summary>
        Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}
