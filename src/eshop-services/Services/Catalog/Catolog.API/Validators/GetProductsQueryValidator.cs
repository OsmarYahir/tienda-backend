using Catalog.API.Models.Products.GetProduct;
using FluentValidation;

namespace Catalog.API.Validators
{
    public class GetProductsQueryValidator: AbstractValidator<GetProductsQuery>
    {
        public GetProductsQueryValidator() 
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("El número de página debe de ser mayor a cero");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("El tamaño de página debe estar 1 y 100");
        }
    }
}
