using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Order.API.Application;
using Order.API.Application.Contracts;

namespace Order.API.Endpoints
{
    public static class OrderEndpoints
    {
        public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/orders").WithTags("Orders");

            group.MapPost("/", CreateOrder)
                .WithName("CreateOrder")
                .WithSummary("Crea una orden a partir de un carrito de compras")
                .WithDescription("Recibe { customerId, basketId } y, opcionalmente, el header Idempotency-Key. " +
                                  "Devuelve 201 si crea una orden nueva, o 200 si repite una Idempotency-Key ya procesada. " +
                                  "Requiere un Bearer token válido (emitido por User.API).")
                .RequireAuthorization()
                .Produces<OrderResponse>(StatusCodes.Status201Created)
                .Produces<OrderResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            group.MapGet("/", GetAll)
                .WithName("GetAllOrders")
                .WithSummary("Lista todas las órdenes (solo Admin)")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .Produces<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden);

            group.MapGet("/{id}", GetById)
                .WithName("GetOrderById")
                .WithSummary("Consulta una orden por Id")
                .Produces<OrderResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound);

            group.MapGet("/customer/{customerId}", GetByCustomer)
                .WithName("GetOrdersByCustomer")
                .WithSummary("Lista las órdenes de un cliente")
                .Produces<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK);

            group.MapPatch("/{id}/status", UpdateStatus)
                .WithName("UpdateOrderStatus")
                .WithSummary("Cambia el estado de una orden validando las transiciones permitidas")
                .Produces<OrderResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound);

            return app;
        }

        private static async Task<IResult> CreateOrder(
            CreateOrderRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IValidator<CreateOrderRequest> validator,
            IOrderService orderService,
            CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var (order, isNew) = await orderService.CreateOrderAsync(request, idempotencyKey, cancellationToken);
            var response = OrderResponse.FromDomain(order);

            return isNew
                ? Results.Created($"/api/orders/{order.Id}", response)
                : Results.Ok(response);
        }

        private static async Task<IResult> GetAll(
            IOrderService orderService, CancellationToken cancellationToken)
        {
            var orders = await orderService.GetAllAsync(cancellationToken);
            return Results.Ok(orders.Select(OrderResponse.FromDomain).ToList());
        }

        private static async Task<IResult> GetById(
            string id, IOrderService orderService, CancellationToken cancellationToken)
        {
            var order = await orderService.GetByIdAsync(id, cancellationToken);
            return Results.Ok(OrderResponse.FromDomain(order));
        }

        private static async Task<IResult> GetByCustomer(
            string customerId, IOrderService orderService, CancellationToken cancellationToken)
        {
            var orders = await orderService.GetByCustomerIdAsync(customerId, cancellationToken);
            return Results.Ok(orders.Select(OrderResponse.FromDomain).ToList());
        }

        private static async Task<IResult> UpdateStatus(
            string id,
            UpdateOrderStatusRequest request,
            IValidator<UpdateOrderStatusRequest> validator,
            IOrderService orderService,
            CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var order = await orderService.UpdateStatusAsync(id, request.Status, cancellationToken);
            return Results.Ok(OrderResponse.FromDomain(order));
        }
    }
}
