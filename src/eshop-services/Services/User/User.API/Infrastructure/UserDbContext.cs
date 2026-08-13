using Microsoft.EntityFrameworkCore;

namespace User.API.Infrastructure
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<Domain.User> Users => Set<Domain.User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Domain.User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                // Único a nivel de base de datos: segunda línea de defensa además de la
                // verificación explícita en AuthService.RegisterAsync (protege contra
                // condiciones de carrera entre dos registros concurrentes con el mismo email).
                entity.HasIndex(u => u.Email).IsUnique();

                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(32);
            });
        }
    }
}
