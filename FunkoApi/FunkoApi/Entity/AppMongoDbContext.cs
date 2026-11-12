using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace FunkoApi.Entity;

public class AppMongoDbContext : DbContext{

    public AppMongoDbContext(DbContextOptions<AppMongoDbContext> options) : base(options)
    {
        // Evita fallos con transacciones (MongoDB local sin replica set)
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }
    
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Proveedor>().ToCollection("proveedores");
    }
    
    // evita fallos para transacciones relaciones
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { 
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never; 
    }
}