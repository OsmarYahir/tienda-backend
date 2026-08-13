using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace User.API.Infrastructure
{
    // Usado SOLO por la herramienta `dotnet ef` (migrations add / database update).
    // Program.cs exige la variable de entorno ConnectionStrings__UserDb en runtime real,
    // pero generar una migración no necesita una conexión viva ni la app completa: solo
    // necesita construir el modelo. Sin esta factory, `dotnet ef migrations add` fallaría
    // porque intentaría ejecutar Program.cs y este lanzaría la excepción de configuración
    // antes de registrar el DbContext.
    public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5434;Database=UserDb;Username=postgres;Password=postgres");
            return new UserDbContext(optionsBuilder.Options);
        }
    }
}
