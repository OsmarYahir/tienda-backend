using FluentValidation;
using User.API.Application;
using User.API.Application.Contracts;

namespace User.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Auth");

            group.MapPost("/register", Register)
                .WithName("Register")
                .WithSummary("Registra un nuevo cliente")
                .WithDescription("Crea la cuenta con rol 'Customer' por defecto y el password ya hasheado (BCrypt).")
                .Produces<UserResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status409Conflict);

            group.MapPost("/login", Login)
                .WithName("Login")
                .WithSummary("Autentica a un usuario y devuelve un JWT")
                .Produces<AuthResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            return app;
        }

        private static async Task<IResult> Register(
            RegisterRequest request,
            IValidator<RegisterRequest> validator,
            IAuthService authService,
            CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var user = await authService.RegisterAsync(request, cancellationToken);
            return Results.Created($"/api/users/{user.Id}", user);
        }

        private static async Task<IResult> Login(
            LoginRequest request,
            IValidator<LoginRequest> validator,
            IAuthService authService,
            CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var result = await authService.LoginAsync(request, cancellationToken);
            return Results.Ok(result);
        }
    }
}
