using FluentValidation;
using Order.API.Domain;

namespace Order.API.Application.Contracts
{
    public record UpdateOrderStatusRequest(OrderStatus Status);

    public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
    {
        public UpdateOrderStatusRequestValidator()
        {
            RuleFor(x => x.Status).IsInEnum().WithMessage("El estado enviado no es válido.");
        }
    }
}
