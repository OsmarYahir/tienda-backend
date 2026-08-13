namespace User.API.Application.Security
{
    public interface ITokenService
    {
        string GenerateToken(Domain.User user);
    }
}
