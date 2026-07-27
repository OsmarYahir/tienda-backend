using Catalog.API.Models.Products.GetProduct;
using Carter;
using Mapster;
using MediatR;
using Catalog.API.Common.Pagination;
// Asegúrate de tener los using necesarios para Product e IEnumerable si están en otro namespace

namespace Catalog.API.Models.Products.GetProduct
{
   
    public record GetProductsResponse(PaginatedResult<Product> Products);

    public class GetProductsEndPoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products", async (
                int pageNumber,
                int pageSize,
                ISender sender) =>
            {
                var query = new GetProductsQuery(
                    pageNumber,
                    pageSize);

                var result = await sender.Send(query);

               
                var response = result.Adapt<GetProductsResponse>();

                return Results.Ok(response);
            })
            .WithName("GetProducts")
            .Produces<GetProductsResponse>(StatusCodes.Status200OK) 
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Get Resumen")
            .WithDescription("Get Resumen");
        }
    }
}