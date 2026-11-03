using FunkoApi.Dto.Funko;
using FunkoApi.Models;

namespace FunkoApi.Mapper;

public static class FunkoMapper {
    
    private const string DefaultImage = "/images/default-funko.png";

    // model -> resposedto
    public static ResponseFunkoDto ToResponseDto(this Funko funko, Proveedor proveedor) { 
        return new ResponseFunkoDto( 
            Id: funko.Id, 
            Nombre: funko.Nombre, 
            Precio: funko.Precio, 
            Stock: funko.Stock, 
            string.IsNullOrWhiteSpace(funko.Imagen) ? DefaultImage : funko.Imagen, 
            ResponseCategoria: funko.Categoria.ToResponseDto(), 
            ResponseProveedor: proveedor.ToResponseDto(), 
            CreatedAt: funko.CreatedAt, 
            UpdatedAt: funko.UpdatedAt 
        ); 
    }
    
    // createdto -> model
    public static Funko ToEntity(this CreateFunkoDto dto) {
        return new Funko {
            Nombre = dto.Nombre,
            Precio = dto.Precio, 
            Stock = dto.Stock, 
            CategoriaId = dto.CategoriaId, 
            ProveedorId = dto.ProveedorId, 
            Imagen = string.IsNullOrWhiteSpace(dto.Imagen) ? null : dto.Imagen, 
            CreatedAt = DateTime.UtcNow
        };
    }
    
    // updatedto -> model
    public static void UpdateEntity(this UpdateFunkoDto dto, Funko funko) {
        funko.Nombre = dto.Nombre;
        funko.Precio = dto.Precio;
        funko.Stock = dto.Stock;
        funko.CategoriaId = dto.CategoriaId;
        funko.ProveedorId = dto.ProveedorId;
        funko.Imagen = string.IsNullOrWhiteSpace(dto.Imagen) ? null : dto.Imagen;
        funko.UpdatedAt = DateTime.UtcNow;
    }
}