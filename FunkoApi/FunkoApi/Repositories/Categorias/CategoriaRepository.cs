using FunkoApi.Entity;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repositories.Categorias;

public class CategoriaRepository(AppDbContext db) : ICategoriaRepository {
    
    public async Task<IReadOnlyList<Categoria>> GetAllAsync() =>
        await db.Categorias.AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<Categoria>> GetAllPagedAsync(int page = 1, int pageSize = 10) =>
        await db.Categorias 
            .AsNoTracking() 
            .OrderBy(c => c.Nombre) 
            .Skip((page - 1) * pageSize) 
            .Take(pageSize) 
            .ToListAsync();

    public async Task<Categoria?> GetByIdAsync(string id) =>
        await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    public async Task<Categoria> AddAsync(Categoria categoria) {
        categoria.CreatedAt = DateTime.UtcNow;
        categoria.IsDeleted = false;
        
        // aqui se inserta y se genera el id estilo yt gracias al ValueGenerator
        await db.Categorias.AddAsync(categoria);
        await db.SaveChangesAsync();
        return categoria;
    }

    public async Task<Categoria?> UpdateAsync(Categoria categoria) {
        var existente = await db.Categorias.FindAsync(categoria.Id);
        if (existente is null) return null;
        
        existente.Nombre = categoria.Nombre;
        existente.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync();
        return existente;
    }

    public async Task<bool> DeleteAsync(string id) {
        var categoria = await db.Categorias.FindAsync(id);
        if (categoria is null) return false;
        
        categoria.IsDeleted = true;
        categoria.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}