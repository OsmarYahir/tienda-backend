using Marten;

namespace Catalog.API.Models.Products.CreateProduct
{

    public record CreateProductComman(string Name, string Description,
        List<string> Category, string ImageFiles, decimal Price)
        : ICommand<CreateProductResul>;

    public record CreateProductResul(Guid Id);
    internal class CreateProductCommandHandler(IDocumentSession documentSession):
        ICommandHandler<CreateProductComman,
            CreateProductResul>
    {
        public async Task<CreateProductResul> Handle(CreateProductComman request,
            CancellationToken cancellationToken)
        {
            Product product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                ImageFiles = request.ImageFiles,
                Price = request.Price

            };

            documentSession.Store(product);
            await documentSession.SaveChangesAsync(cancellationToken);
            return new CreateProductResul(product.Id);
        }
    }
}
