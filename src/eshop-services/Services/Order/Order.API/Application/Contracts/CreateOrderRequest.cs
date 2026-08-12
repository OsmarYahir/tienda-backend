using FluentValidation;

namespace Order.API.Application.Contracts
{
    public record CreateOrderRequest(string CustomerId, string BasketId);

    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty().WithMessage("El customerId es requerido.");
            RuleFor(x => x.BasketId).NotEmpty().WithMessage("El basketId es requerido.");
        }
    }
}
