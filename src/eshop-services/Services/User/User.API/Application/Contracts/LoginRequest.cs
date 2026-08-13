using FluentValidation;

namespace User.API.Application.Contracts
{
    public record LoginRequest(string Email, string Password);

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("El email no es válido.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es requerida.");
        }
    }
}
