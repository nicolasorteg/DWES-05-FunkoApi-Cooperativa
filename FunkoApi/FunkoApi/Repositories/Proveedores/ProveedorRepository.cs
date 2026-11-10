using FunkoApi.Entity;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace FunkoApi.Repositories.Proveedores;

public class ProveedorRepository(AppMongoDbContext db) : IProveedorRepository {

    public async Task<IReadOnlyList<Proveedor>> GetAllAsync() =>
        await db.Proveedores.AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<Proveedor>> GetAllPagedAsync(int page = 1, int pageSize = 10) =>
        await db.Proveedores 
            .AsNoTracking() 
            .OrderBy(p => p.Nombre) 
            .Skip((page - 1) * pageSize) 
            .Take(pageSize) 
            .ToListAsync();

    public async Task<Proveedor?> GetByIdAsync(string id) {
        if (!ObjectId.TryParse(id, out var objectId)) return null; // parse id -> objectid
        return await db.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == objectId && !p.IsDeleted);
    }

    public async Task<Proveedor> AddAsync(Proveedor entity) {

        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsDeleted = false;

        await db.Proveedores.AddAsync(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<Proveedor?> UpdateAsync(Proveedor entity) {
        
        var existente = await db.Proveedores.FindAsync(entity.Id); 
        if (existente is null) return null; 
        
        existente.Nombre = entity.Nombre; 
        existente.Email = entity.Email; 
        existente.Telefono = entity.Telefono;
        existente.UpdatedAt = DateTime.UtcNow; 
        
        await db.SaveChangesAsync();
        return existente;
    }

    public async Task<bool> DeleteAsync(string id) {
        if (!ObjectId.TryParse(id, out var objectId)) return false;

        var proveedor = await db.Proveedores.FindAsync(objectId);
        if (proveedor is null) return false;

        proveedor.IsDeleted = true;
        proveedor.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }
}