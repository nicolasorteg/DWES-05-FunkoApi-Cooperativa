using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dto.Proveedor;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Mapper;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Repositories.Proveedores;
using FunkoApi.Services.Cache;
using FunkoApi.Services.Proveedor;

namespace FunkoApi.Services.Proveedores;

public class ProveedorService (
    IProveedorRepository proveedorRepository,
    IFunkosRepository funkoRepository,
    IValidator<CreateProveedorDto> createValidator,
    IValidator<UpdateProveedorDto> updateValidator,
    ICacheService cache) : IProveedorService
{
    
    private static long _pagedCacheVersion;
    private static string CacheKeyPaged(int page, int pageSize) =>
        $"proveedores:paged:v{Volatile.Read(ref _pagedCacheVersion)}:{page}:{pageSize}";  
    private static void InvalidatePagedCache() => Interlocked.Increment(ref _pagedCacheVersion);
    
    private static string CacheKeyItem(string id) => $"proveedor:{id}";

    public async Task<Result<IReadOnlyList<ResponseProveedorDto>, DomainError>> GetAllPagedAsync(int page = 1, int pageSize = 10) {

        var cacheKey = CacheKeyPaged(page, pageSize);
        
        // primero busca en cache
        var cached = await cache.GetAsync<IReadOnlyList<ResponseProveedorDto>>(cacheKey);
        if (cached != null)
            return Result.Success<IReadOnlyList<ResponseProveedorDto>, DomainError>(cached);

        // luego llama repository
        var proveedores = await proveedorRepository.GetAllPagedAsync(page, pageSize);
        var dtos = proveedores.Select(p => p.ToResponseDto()).ToList();

        // set en la cache
        await cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(10));
        return Result.Success<IReadOnlyList<ResponseProveedorDto>, DomainError>(dtos);
    }

    public async Task<Result<ResponseProveedorDto, DomainError>> GetByIdAsync(string id) {

        var cacheKey = CacheKeyItem(id);

        // cache
        var cached = await cache.GetAsync<ResponseProveedorDto>(cacheKey);
        if (cached != null)
            return Result.Success<ResponseProveedorDto, DomainError>(cached);

        // bd
        var proveedor = await proveedorRepository.GetByIdAsync(id);
        if (proveedor == null)
            return Result.Failure<ResponseProveedorDto, DomainError>(ModelErrors.NotFound(id));

        // mapear y guardar en caché
        var dto = proveedor.ToResponseDto();
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
        return Result.Success<ResponseProveedorDto, DomainError>(dto);
    }

    public async Task<Result<ResponseProveedorDto, DomainError>> AddAsync(CreateProveedorDto dto) {

        // validar
        var validationResult = await createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) {
            var errorMsg = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<ResponseProveedorDto, DomainError>(ModelErrors.Validation(errorMsg));
        }

        // mapear y guardar
        var entity = dto.ToEntity();
        var creado = await proveedorRepository.AddAsync(entity);

        await cache.RemoveAsync(CacheKeyItem(entity.Id.ToString())); 
        InvalidatePagedCache();
        return Result.Success<ResponseProveedorDto, DomainError>(creado.ToResponseDto());
    }

    public async Task<Result<ResponseProveedorDto, DomainError>> UpdateAsync(string id, UpdateProveedorDto dto) {

        // validar
        var validationResult = await updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) {
            var errorMsg = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<ResponseProveedorDto, DomainError>(ModelErrors.Validation(errorMsg));
        }

        // comprobar existencia
        var existente = await proveedorRepository.GetByIdAsync(id);
        if (existente == null)
            return Result.Failure<ResponseProveedorDto, DomainError>(ModelErrors.NotFound(id));

        // mapear y actualizar
        dto.UpdateEntity(existente);
        var actualizado = await proveedorRepository.UpdateAsync(existente);
        if (actualizado == null)
            return Result.Failure<ResponseProveedorDto, DomainError>(ModelErrors.NotFound(id));

        await cache.RemoveAsync(CacheKeyItem(id));
        InvalidatePagedCache();
        return Result.Success<ResponseProveedorDto, DomainError>(actualizado.ToResponseDto());
    }

    public async Task<UnitResult<DomainError>> DeleteAsync(string id) {

        // comprobar existencia
        var existente = await proveedorRepository.GetByIdAsync(id);
        if (existente == null)
            return UnitResult.Failure<DomainError>(ModelErrors.NotFound(id));

        // comprobar si tiene funko asociado
        var funkosAsociados = await funkoRepository.ExistsByProveedorAsync(id);
        if (funkosAsociados) {
            return UnitResult.Failure<DomainError>(ModelErrors.FunkoAsociado());
        }

        // borrado lógico
        var borrado = await proveedorRepository.DeleteAsync(id);
        if (!borrado)
            return UnitResult.Failure<DomainError>(ModelErrors.NotFound(id));

        // invalidar cachés
        await cache.RemoveAsync(CacheKeyItem(id));
        InvalidatePagedCache();

        return UnitResult.Success<DomainError>();
    }
}