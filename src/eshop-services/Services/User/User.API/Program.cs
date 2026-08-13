using FluentValidation;
using Microsoft.EntityFrameworkCore;
using User.API.Application;
using User.API.Application.Contracts;
using User.API.Application.Security;
using User.API.Endpoints;
using User.API.Exceptions;
using User.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) PostgreSQL: cadena de conexión desde variables de entorno.
//    ConnectionStrings__UserDb = Host=...;Port=5432;Database=UserDb;Username=...;Password=...
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("UserDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "PostgreSQL no está configurado. Define la variable de entorno 'ConnectionStrings__UserDb'.");

builder.Services.AddDbContext<UserDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ---------------------------------------------------------------------------
// 2) JWT: clave/issuer/audience desde variables de entorno, nunca en el código.
//    Jwt__SecretKey, Jwt__Issuer, Jwt__Audience, Jwt__ExpirationMinutes (opcional)
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<ITokenService, TokenService>();

// ---------------------------------------------------------------------------
// 3) Validación (FluentValidation)
// ---------------------------------------------------------------------------
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ---------------------------------------------------------------------------
// 4) CORS: mismo criterio que Order.API — whitelist configurable, con fallback
//    a los orígenes locales de desarrollo.
// ---------------------------------------------------------------------------
const string CorsPolicyName = "AllowedClients";
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
// 5) Manejo global de errores: 400/401/409 según la regla de negocio incumplida,
//    500 genérico que jamás expone stack trace ni detalles internos.
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
        Title = "User.API",
        Version = "v1",
        Description = "Registro, autenticación y emisión de JWT."
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Aplica migraciones pendientes al arrancar (igual de conveniente que
// AutoCreateSchemaObjects en Catalog.API, pero explícito vía EF Core Migrations).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<UserDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User.API v1");
    });
}

app.UseCors(CorsPolicyName);
app.UseExceptionHandler();

app.MapAuthEndpoints();
app.MapHealthChecks("/health");

app.Run();
