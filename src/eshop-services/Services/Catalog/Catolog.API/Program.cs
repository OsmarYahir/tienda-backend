using Catalog.API.Behaviors;
using Catalog.API.Exeptions;
using JasperFx;
using Marten;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddMemoryCache();


builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);


builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (origins is { Length: > 0 })
        options.AddDefaultPolicy(policy => policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader());
    else
        options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddCarter();
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("CatalogDb")!);
    opts.AutoCreateSchemaObjects = AutoCreate.All;
});




var app = builder.Build();

app.UseCors();

/*utilizamos carter como parte de minimal api*/
app.UseExceptionHandler();

app.MapCarter();

app.Run();
