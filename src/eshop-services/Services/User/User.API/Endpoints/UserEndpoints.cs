using User.API.Application.Contracts;
using User.API.Exceptions;
using User.API.Infrastructure;

namespace User.API.Endpoints
{
    public static class UserEndpoints
    {
        public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

            group.MapGet("/{id:guid}", GetById)
                .WithName("GetUserById")
                .WithSummary("Consulta el perfil público de un usuario (id, email, rol)")
                .WithDescription("Usado por Ticket.API para mostrar el email real del cliente en el ticket, " +
                                  "en vez de su Id. No expone PasswordHash. Requiere estar autenticado.")
                .Produces<UserResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound);

            return app;
        }

        private static async Task<IResult> GetById(Guid id, IUserRepository repository, CancellationToken cancellationToken)
        {
            var user = await repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("User", id);

            return Results.Ok(UserResponse.FromDomain(user));
        }
    }
}
