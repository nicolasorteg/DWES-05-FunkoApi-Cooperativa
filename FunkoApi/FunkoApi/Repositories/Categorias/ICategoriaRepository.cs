using FunkoApi.Models;
using FunkoApi.Repositories.Common;

namespace FunkoApi.Repositories.Categorias;

/// <summary>
/// interfaz para categoria repository
/// </summary>
public interface ICategoriaRepository : ICrudRepository<Categoria, string> { }