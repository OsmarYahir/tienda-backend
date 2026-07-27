namespace Catalog.API.Models.Products.DeleteProduct
{

    public record DeleteProductResponde(bool IsSuccess);
    public class DeleteProductEndPoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("/products/{id}", async (Guid id, ISender sender) =>
            {
                var result = await sender.Send(new DeleteProductCommand(id));
                var responde = result.Adapt<DeleteProductResponde>();
                return Results.Ok(responde);
            })
                .WithName("DeleteProduct")
                .Produces<DeleteProductResponde>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Borrar Producto")
                .WithDescription("Eliminar producto");
        }
    }
}
