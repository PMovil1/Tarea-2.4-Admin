using Bitacora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bitacora.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Registro> Registros => Set<Registro>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(u => u.Email).IsRequired().HasMaxLength(254);
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Registro>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FotoBase64);
            entity.Property(e => e.FechaCreacion).IsRequired();

            // Usamos 'entity' directamente en lugar de repetir modelBuilder.Entity<Registro>()
            entity.HasOne(r => r.Usuario)
                  .WithMany(u => u.Registros)
                  .HasForeignKey(r => r.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
