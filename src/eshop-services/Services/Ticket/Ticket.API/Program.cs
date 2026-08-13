using QuestPDF.Infrastructure;
using Ticket.API.Application.Orders;
using Ticket.API.Application.Pdf;
using Ticket.API.Endpoints;
using Ticket.API.Exceptions;

// QuestPDF requiere declarar la licencia una sola vez, al arrancar. Community es gratuita
// para individuos/empresas pequeñas — ver https://www.questpdf.com/license/ si el proyecto
// deja de calificar.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) Cliente HTTP hacia Order.API, con propagación del JWT entrante.
//    OrderApi:BaseUrl (env var: OrderApi__BaseUrl)
// ---------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationPropagationHandler>();

var orderApiBaseUrlConfig = builder.Configuration["OrderApi:BaseUrl"];
var orderApiBaseUrl = string.IsNullOrWhiteSpace(orderApiBaseUrlConfig)
    ? "http://localhost:8082"
    : orderApiBaseUrlConfig;

builder.Services.AddHttpClient<IOrderApiClient, OrderApiClient>(client =>
{
    client.BaseAddress = new Uri(orderApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<AuthorizationPropagationHandler>();

// ---------------------------------------------------------------------------
// 2) Generador de PDF
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<ITicketPdfGenerator, TicketPdfGenerator>();

// ---------------------------------------------------------------------------
// 3) CORS: mismo criterio que el resto de los servicios.
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
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173");

        policy.AllowAnyMethod().WithHeaders("Content-Type", "Authorization");
    });
});

// ---------------------------------------------------------------------------
// 4) Manejo global de errores
// ---------------------------------------------------------------------------
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------------------------------------------------------------------------
// 5) Swagger / OpenAPI
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Ticket.API",
        Version = "v1",
        Description = "Generación de tickets/recibos en PDF a partir de una orden de Order.API."
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Token emitido por User.API. Ticket.API no lo valida: lo reenvía tal cual a Order.API."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket.API v1");
    });
}

app.UseCors(CorsPolicyName);
app.UseExceptionHandler();

app.MapTicketEndpoints();
app.MapHealthChecks("/health");

app.Run();
