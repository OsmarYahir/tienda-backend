namespace User.API.Infrastructure
{
    public interface IUserRepository
    {
        Task<Domain.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task AddAsync(Domain.User user, CancellationToken cancellationToken = default);
    }
}
