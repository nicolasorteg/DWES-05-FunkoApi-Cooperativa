using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace FunkoApi.Entity;

public class AppMongoDbContext(DbContextOptions<AppMongoDbContext> options) : DbContext(options) {

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