using FunkoApi.IdGenerators;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Entity;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Funko> Funkos { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>(e => {
            e.ToTable("Categorias");
            e.HasKey(c => c.Id);
            
            e.Property(c => c.Id)
                .HasMaxLength(11)
                .HasValueGenerator<YouTubeIdValueGenerator>()
                .ValueGeneratedOnAdd();

            e.Property(c => c.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            e.Property(c => c.CreatedAt)
                .IsRequired();
            
            e.Property(c => c.DeletedAt)
                .HasDefaultValue(false);

            e.HasQueryFilter(c => !c.IsDeleted);
        });
    }
}