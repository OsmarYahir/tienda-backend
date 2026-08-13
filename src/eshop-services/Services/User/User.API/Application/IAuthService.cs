using User.API.Application.Contracts;

namespace User.API.Application
{
    public interface IAuthService
    {
        Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }
}
