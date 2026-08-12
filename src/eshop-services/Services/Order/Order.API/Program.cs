using System.Text.Json.Serialization;
using FluentValidation;
using Order.API.Application;
using Order.API.Application.Basket;
using Order.API.Application.Contracts;
using Order.API.Endpoints;
using Order.API.Exceptions;
using Order.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) MongoDB Atlas: la cadena de conexión NUNCA se hardcodea. Se lee de
//    configuración, la cual en runtime se resuelve con variables de entorno:
//      MongoDb__ConnectionString = mongodb+srv://usuario:password@cluster.mongodb.net
//      MongoDb__DatabaseName     = OrdersDb
//      MongoDb__OrdersCollectionName = orders   (opcional, tiene default)
// ---------------------------------------------------------------------------
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.AddSingleton<OrdersDbContext>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ---------------------------------------------------------------------------
// 2) Cliente HTTP hacia Basket.API (para obtener el carrito al crear la orden)
// ---------------------------------------------------------------------------
// appsettings.json trae "BasketApi:BaseUrl" = "" (a propósito, para no quemar una URL de
// entorno ahí). Un simple "??" NO cubre ese caso: solo reemplaza null, y una cadena vacía
// desde appsettings.json no es null, así que el fallback nunca se aplicaba y `new Uri("")`
// tumbaba el servicio con UriFormatException en cuanto se resolvía IBasketClient.
var basketApiBaseUrlConfig = builder.Configuration["BasketApi:BaseUrl"];
var basketApiBaseUrl = string.IsNullOrWhiteSpace(basketApiBaseUrlConfig)
    ? "http://localhost:5122"
    : basketApiBaseUrlConfig;
builder.Services.AddHttpClient<IBasketClient, BasketClient>(client =>
{
    client.BaseAddress = new Uri(basketApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ---------------------------------------------------------------------------
// 3) Validación (FluentValidation)
// ---------------------------------------------------------------------------
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// Los enums viajan como texto ("Pending"/"Confirmed"/"Cancelled") en el JSON,
// más legibles para un frontend en React que un índice numérico.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ---------------------------------------------------------------------------
// 4) CORS: habilita que el frontend React consuma la API, incluyendo el envío
//    del header custom "Idempotency-Key" (si no se declara explícitamente,
//    el navegador lo bloquea en el preflight).
// ---------------------------------------------------------------------------
const string CorsPolicyName = "ReactClient";
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins is { Length: > 0 })
            policy.WithOrigins(allowedOrigins);
        else
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173"); // CRA / Vite

        policy
            .AllowAnyMethod()
            .WithHeaders("Content-Type", "Idempotency-Key", "Authorization")
            .WithExposedHeaders("Location");
    });
});

// ---------------------------------------------------------------------------
// 5) Manejo global de errores: 400 (validación/reglas de negocio), 404, y 500
//    genérico que jamás expone stack trace ni detalles internos (ver
//    CustomExceptionHandler).
// ---------------------------------------------------------------------------
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------------------------------------------------------------------------
// 6) Swagger / OpenAPI
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Order.API",
        Version = "v1",
        Description = "Microservicio de Órdenes de Compra (Fase 2) — MongoDB Atlas + Minimal API"
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order.API v1");
    });
}

app.UseCors(CorsPolicyName);
app.UseExceptionHandler();

app.MapOrderEndpoints();
app.MapHealthChecks("/health");

app.Run();
