using FunkoApi.Entity;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace FunkoApi.Seed;

/// <summary>
/// Clase encargada del seed incial de datos
/// </summary>
public class DbSeeder {
    
    public static readonly string ProveedorDefaultId = "672f1a8b3c9d4e5f6a7b8c9d"; 
    public static readonly string ProveedorSecundarioId = "672f1a8b3c9d4e5f6a7b8c9e";

    public static async Task SeedAsync(AppDbContext db, AppMongoDbContext dbMongo, ILogger logger) {
       
        // categorias
        if (!await db.Categorias.AnyAsync()) {
            
            var fecha = DateTime.UtcNow;
            db.Categorias.AddRange(
                new Categoria { Id = "CatAnime001", Nombre = "Anime", CreatedAt = fecha, IsDeleted = false },
                new Categoria { Id = "CatGame0001", Nombre = "Videojuegos", CreatedAt = fecha, IsDeleted = false },
                new Categoria { Id = "CatMovie001", Nombre = "Películas & Series", CreatedAt = fecha, IsDeleted = false }
            );
            await db.SaveChangesAsync();
        }

        // proveedores
        if (!await dbMongo.Proveedores.AnyAsync()) {
            
            var fecha = DateTime.UtcNow;
            dbMongo.Proveedores.AddRange(
                new Proveedor 
                { 
                    Id = ObjectId.Parse(ProveedorDefaultId), 
                    Nombre = "Distribuidores Funko España S.L.", 
                    Email = "contacto@funkodist.es", 
                    Telefono = "+34912345678", 
                    CreatedAt = fecha, 
                    IsDeleted = false 
                },
                new Proveedor 
                { 
                    Id = ObjectId.Parse(ProveedorSecundarioId), 
                    Nombre = "Global Pop Collectibles Ltd.", 
                    Email = "sales@globalpop.com", 
                    Telefono = "+442079460912", 
                    CreatedAt = fecha, 
                    IsDeleted = false 
                }
            );
            await dbMongo.SaveChangesAsync();
        }

        // funkos
        if (!await db.Funkos.AnyAsync()) {
            
            var fecha = DateTime.UtcNow;
            db.Funkos.AddRange(
                new Funko 
                { 
                    Nombre = "Luffy", 
                    Precio = 15.99m, 
                    Stock = 20, 
                    Imagen = "/uploads/images/default-funko.png", 
                    CategoriaId = "CatAnime001", 
                    ProveedorId = ProveedorDefaultId, 
                    CreatedAt = fecha, 
                    IsDeleted = false 
                },
                new Funko 
                { 
                    Nombre = "Geralt de Rivia", 
                    Precio = 18.50m, 
                    Stock = 8, 
                    Imagen = "/uploads/images/default-funko.png",
                    CategoriaId = "CatGame0001", 
                    ProveedorId = ProveedorDefaultId, 
                    CreatedAt = fecha, 
                    IsDeleted = false 
                },
                new Funko 
                { 
                    Nombre = "Darth Vader", 
                    Precio = 21.00m, 
                    Stock = 5, 
                    Imagen = "/uploads/images/default-funko.png",
                    CategoriaId = "CatMovie001", 
                    ProveedorId = ProveedorSecundarioId, 
                    CreatedAt = fecha, 
                    IsDeleted = false 
                },
                new Funko 
                { 
                    Nombre = "Goku", 
                    Precio = 14.25m, 
                    Stock = 15, 
                    Imagen = "/uploads/images/default-funko.png",
                    CategoriaId = "CatAnime001", 
                    ProveedorId = ProveedorSecundarioId, 
                    CreatedAt = fecha, 
                    IsDeleted = false 
                }
            );
            await db.SaveChangesAsync();
        }
    }
}