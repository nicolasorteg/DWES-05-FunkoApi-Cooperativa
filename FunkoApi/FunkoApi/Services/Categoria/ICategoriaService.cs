using FunkoApi.Dto.Categoria;
using FunkoApi.Services.Common;

namespace FunkoApi.Services.Categoria;

public interface ICategoriaService : 
    ICrudService<ResponseCategoriaDto, CreateCategoriaDto, UpdateCategoriaDto, string> { }