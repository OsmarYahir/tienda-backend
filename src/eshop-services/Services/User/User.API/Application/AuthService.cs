using User.API.Application.Contracts;
using User.API.Application.Security;
using User.API.Exceptions;
using User.API.Infrastructure;

namespace User.API.Application
{
    public class AuthService(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService) : IAuthService
    {
        public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await repository.GetByEmailAsync(request.Email, cancellationToken);
            if (existing is not null)
                throw new ConflictException($"Ya existe una cuenta registrada con el correo '{request.Email}'.");

            var passwordHash = passwordHasher.Hash(request.Password);
            var user = Domain.User.Register(request.Email, passwordHash);

            await repository.AddAsync(user, cancellationToken);

            return UserResponse.FromDomain(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await repository.GetByEmailAsync(request.Email, cancellationToken);

            // Mismo mensaje genérico tanto si el email no existe como si la contraseña es
            // incorrecta: no le da a un atacante pista de cuál de las dos falló
            // (evita enumeración de cuentas registradas).
            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Credenciales inválidas.");

            var token = tokenService.GenerateToken(user);
            return new AuthResponse(token, UserResponse.FromDomain(user));
        }
    }
}
