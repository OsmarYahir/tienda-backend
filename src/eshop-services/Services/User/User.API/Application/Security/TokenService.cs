using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace User.API.Application.Security
{
    public class TokenService(IOptions<JwtSettings> options) : ITokenService
    {
        private readonly JwtSettings _settings = options.Value;

        public string GenerateToken(Domain.User user)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
                throw new InvalidOperationException(
                    "JWT no está configurado. Define las variables de entorno 'Jwt__SecretKey', 'Jwt__Issuer' y 'Jwt__Audience'.");

            // Nombres de claim CORTOS y literales ("sub", "email", "role"): si se usara
            // ClaimTypes.Role en vez de un Claim("role", ...) a mano, el payload del JWT
            // terminaría con la URI larga de .NET (.../identity/claims/role) en vez de la
            // clave "role" pedida explícitamente.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
