using FunkoApi.Entity;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repositories.Funkos;

/// <summary>
/// Implementación EF Core del repositorio de Funkos.
/// Aplica filtrado por soft-delete (IsDeleted) en todas las lecturas.
/// </summary>
public sealed class FunkosRepository(AppDbContext context) : IFunkosRepository
{
    public async Task<IReadOnlyList<Funko>> GetAllAsync() =>
        await context.Funkos
            .AsNoTracking()
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.Nombre)
            .ToListAsync();

    public async Task<IReadOnlyList<Funko>> GetAllPagedAsync(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await context.Funkos
            .AsNoTracking()
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.Nombre)
            .ThenBy(f => f.Id)          // desempate estable entre páginas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Funko?> GetByIdAsync(long id) =>
        await context.Funkos
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

    public async Task<Funko> AddAsync(Funko entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.IsDeleted = false;

        await context.Funkos.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<Funko?> UpdateAsync(Funko entity)
    {
        var existing = await context.Funkos
            .FirstOrDefaultAsync(f => f.Id == entity.Id && !f.IsDeleted);
        if (existing is null) return null;

        // Copiamos solo los campos mutables (no Id, CreatedAt ni soft-delete).
        existing.Nombre      = entity.Nombre;
        existing.Precio      = entity.Precio;
        existing.Stock       = entity.Stock;
        existing.Imagen      = entity.Imagen;
        existing.CategoriaId = entity.CategoriaId;
        existing.ProveedorId = entity.ProveedorId;
        existing.UpdatedAt   = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var funko = await context.Funkos
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (funko is null) return false;

        // Soft-delete coherente con el modelo (IsDeleted / DeletedAt).
        funko.IsDeleted = true;
        funko.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsByCategoriaAsync(string categoriaId) =>
        context.Funkos.AnyAsync(f => f.CategoriaId == categoriaId && !f.IsDeleted);

    public Task<bool> ExistsByProveedorAsync(string proveedorId) =>
        context.Funkos.AnyAsync(f => f.ProveedorId == proveedorId && !f.IsDeleted);
}