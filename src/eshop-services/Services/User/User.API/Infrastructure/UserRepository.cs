using Microsoft.EntityFrameworkCore;

namespace User.API.Infrastructure
{
    public class UserRepository(UserDbContext context) : IUserRepository
    {
        public Task<Domain.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return context.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        }

        public async Task AddAsync(Domain.User user, CancellationToken cancellationToken = default)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
