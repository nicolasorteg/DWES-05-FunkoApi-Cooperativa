using FunkoApi.Models;
using FunkoApi.Repositories.Common;

namespace FunkoApi.Repositories.Funkos;

/// <summary>
/// Repositorio específico de <see cref="Funko"/>.
/// Hereda el CRUD genérico y añade paginación/filtrado y comprobaciones
/// de integridad referencial usadas por CategoriaService y ProveedorService.
/// </summary>
public interface IFunkosRepository : ICrudRepository<Funko, long>
{
    /// <summary>
    /// Indica si existe al menos un Funko NO eliminado asociado a la categoría indicada.
    /// </summary>
    Task<bool> ExistsByCategoriaAsync(string categoriaId);

    /// <summary>
    /// Indica si existe al menos un Funko NO eliminado asociado al proveedor indicado.
    /// </summary>
    Task<bool> ExistsByProveedorAsync(string proveedorId);
}