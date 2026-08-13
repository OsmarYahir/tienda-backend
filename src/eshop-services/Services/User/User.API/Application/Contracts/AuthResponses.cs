namespace User.API.Application.Contracts
{
    public record UserResponse(Guid Id, string Email, string Role)
    {
        public static UserResponse FromDomain(Domain.User user) => new(user.Id, user.Email, user.Role);
    }

    public record AuthResponse(string Token, UserResponse User);
}
