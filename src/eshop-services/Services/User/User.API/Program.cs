using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// User.API emite tokens (arriba) y ahora TAMBIÉN los valida: GET /api/users/{id} está
// protegido con RequireAuthorization() (lo usa Ticket.API para resolver el email del
// cliente). Misma configuración que Order.API, incluido MapInboundClaims = false — sin
// eso, RoleClaimType/RequireRole quedan apuntando a un claim que ya no existe después
// del remapeo por defecto de JwtSecurityTokenHandler (bug real que ya encontramos ahí).
var jwtSettingsForAuth = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettingsForAuth.SecretKey))
    throw new InvalidOperationException(
        "JWT no está configurado. Define las variables de entorno 'Jwt__SecretKey', 'Jwt__Issuer' y 'Jwt__Audience'.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettingsForAuth.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettingsForAuth.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettingsForAuth.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "email"
        };
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHealthChecks("/health");

app.Run();
