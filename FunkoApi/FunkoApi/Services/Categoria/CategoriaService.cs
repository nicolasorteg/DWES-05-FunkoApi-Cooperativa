using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dto.Categoria;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Mapper;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Services.Cache;

namespace FunkoApi.Services.Categoria;

public class CategoriaService (
    ICategoriaRepository categoriaRepository,
    IFunkosRepository funkoRepository, 
    IValidator<CreateCategoriaDto> createValidator, 
    IValidator<UpdateCategoriaDto> updateValidator,
    ICacheService cache) : ICategoriaService 
{
    
    private static long _pagedCacheVersion;
    private static string CacheKeyPaged(int page, int pageSize) =>
        $"categorias:paged:v{Volatile.Read(ref _pagedCacheVersion)}:{page}:{pageSize}";  
    private static void InvalidatePagedCache() => Interlocked.Increment(ref _pagedCacheVersion);
    private static string CacheKeyItem(string id) => $"categoria:{id}";
    
    public async Task<Result<IReadOnlyList<ResponseCategoriaDto>, DomainError>> GetAllPagedAsync(int page = 1, int pageSize = 10) {
        
        var cacheKey = CacheKeyPaged(page, pageSize);
        
        // primero busca en cache
        var cached = await cache.GetAsync<IReadOnlyList<ResponseCategoriaDto>>(cacheKey);
        if (cached != null) 
            return Result.Success<IReadOnlyList<ResponseCategoriaDto>, DomainError>(cached);
        
        // luego llama repository
        var categorias = await categoriaRepository.GetAllPagedAsync(page, pageSize); 
        var dtos = categorias.Select(c => c.ToResponseDto()).ToList();
        
        // set en la cache
        await cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(10)); 
        return Result.Success<IReadOnlyList<ResponseCategoriaDto>, DomainError>(dtos);
    }

    public async Task<Result<ResponseCategoriaDto, DomainError>> GetByIdAsync(string id) {
        
        var cacheKey = CacheKeyItem(id);
        
        // cache
        var cached = await cache.GetAsync<ResponseCategoriaDto>(cacheKey);
        if (cached != null)
            return Result.Success<ResponseCategoriaDto, DomainError>(cached);
        
        // bd
        var categoria = await categoriaRepository.GetByIdAsync(id); 
        if (categoria == null) 
            return Result.Failure<ResponseCategoriaDto, DomainError>(ModelErrors.NotFound(id));
        
        // mapear y guardar en caché
        var dto = categoria.ToResponseDto(); 
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10)); 
        return Result.Success<ResponseCategoriaDto, DomainError>(dto);
    }

    public async Task<Result<ResponseCategoriaDto, DomainError>> AddAsync(CreateCategoriaDto dto) {
        
        // validar
        var validationResult = await createValidator.ValidateAsync(dto); 
        if (!validationResult.IsValid) { 
            var errorMsg = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)); 
            return Result.Failure<ResponseCategoriaDto, DomainError>(ModelErrors.Validation(errorMsg));
        }
        
        // mapear y guardar
        var entity = dto.ToEntity(); 
        var creado = await categoriaRepository.AddAsync(entity);
        
        InvalidatePagedCache();
        return Result.Success<ResponseCategoriaDto, DomainError>(creado.ToResponseDto());
    }

    public async Task<Result<ResponseCategoriaDto, DomainError>> UpdateAsync(string id, UpdateCategoriaDto dto) {
        
        // validar
        var validationResult = await updateValidator.ValidateAsync(dto); 
        if (!validationResult.IsValid) { 
            var errorMsg = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<ResponseCategoriaDto, DomainError>(ModelErrors.Validation(errorMsg));
        }
        
        // comprobar existencia
        var existente = await categoriaRepository.GetByIdAsync(id);
        if (existente == null) 
            return Result.Failure<ResponseCategoriaDto, DomainError>(ModelErrors.NotFound(id));

        // mapear y actualizar
        var entityToUpdate = dto.UpdateEntity();
        entityToUpdate.Id = id; 
        var actualizado = await categoriaRepository.UpdateAsync(entityToUpdate);
        if (actualizado == null)
            return Result.Failure<ResponseCategoriaDto, DomainError>(ModelErrors.NotFound(id));
        
        await cache.RemoveAsync(CacheKeyItem(id)); 
        InvalidatePagedCache();
        return Result.Success<ResponseCategoriaDto, DomainError>(actualizado.ToResponseDto());
    }

    public async Task<UnitResult<DomainError>> DeleteAsync(string id) {
        
        // comprobar existencia
        var existente = await categoriaRepository.GetByIdAsync(id); 
        if (existente == null) 
            return UnitResult.Failure<DomainError>(ModelErrors.NotFound(id));
        
        // comprobar si tiene funko asociado
        var funkosAsociados = await funkoRepository.ExistsByCategoriaAsync(id);
        if (funkosAsociados) {
            return UnitResult.Failure<DomainError>(ModelErrors.FunkoAsociado());
        }
        
        // borrado lógico
        var borrado = await categoriaRepository.DeleteAsync(id); 
        if (!borrado) 
            return UnitResult.Failure<DomainError>(ModelErrors.NotFound(id));

        // invalidar cachés
        await cache.RemoveAsync(CacheKeyItem(id));
        InvalidatePagedCache();

        return UnitResult.Success<DomainError>();
    }
}