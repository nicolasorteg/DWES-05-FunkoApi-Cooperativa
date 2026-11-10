using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dto.Funko;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Mapper;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Repositories.Proveedores;
using FunkoApi.Services.Cache;
using FunkoApi.Models;

namespace FunkoApi.Services.Funkos;

/// <summary>
/// Servicio de Funkos: funko y categoría en PostgreSQL (EF Core), proveedor en MongoDB.
/// Usa caché cache-aside solo para GetById (funko:{id}).
/// </summary>
public class FunkoService(
    IFunkosRepository funkoRepository,
    ICategoriaRepository categoriaRepository,
    IProveedorRepository proveedorRepository,
    IValidator<CreateFunkoDto> createValidator,
    IValidator<UpdateFunkoDto> updateValidator,
    ICacheService cache,
    ILogger<FunkoService> logger) : IFunkoService {

    private static string CacheKey(long id) => $"funko:{id}";

    public async Task<Result<IReadOnlyList<ResponseFunkoDto>, DomainError>> GetAllPagedAsync(int page = 1, int pageSize = 10) {
        logger.LogInformation("Obteniendo funkos: página {Page}, tamaño {PageSize}", page, pageSize);

        var funkos = await funkoRepository.GetAllPagedAsync(page, pageSize);

        // un proveedor puede repetirse en varios funkos: se consulta una sola vez
        var proveedores = new Dictionary<string, Models.Proveedor>();
        foreach (var proveedorId in funkos.Select(f => f.ProveedorId).Distinct()) {
            var proveedor = await proveedorRepository.GetByIdAsync(proveedorId);
            if (proveedor is null) {
                logger.LogWarning("El proveedor {ProveedorId} de un funko no existe", proveedorId);
                return Result.Failure<IReadOnlyList<ResponseFunkoDto>, DomainError>(
                    ModelErrors.ProveedorNotFound(proveedorId));
            }
            proveedores[proveedorId] = proveedor;
        }

        IReadOnlyList<ResponseFunkoDto> respuesta = funkos
            .Select(f => f.ToResponseDto(proveedores[f.ProveedorId]))
            .ToList();

        return Result.Success<IReadOnlyList<ResponseFunkoDto>, DomainError>(respuesta);
    }

    public async Task<Result<ResponseFunkoDto, DomainError>> GetByIdAsync(long id) {
        logger.LogInformation("Obteniendo funko {Id}", id);

        // cache-aside: primero la caché
        var cacheKey = CacheKey(id);
        var cached = await cache.GetAsync<ResponseFunkoDto>(cacheKey);
        if (cached is not null) {
            logger.LogDebug("Funko {Id} obtenido de la caché", id);
            return Result.Success<ResponseFunkoDto, DomainError>(cached);
        }

        // si no está, a la BD
        var funko = await funkoRepository.GetByIdAsync(id);
        if (funko is null) {
            logger.LogWarning("Funko {Id} no encontrado", id);
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.NotFound(id.ToString()));
        }

        var proveedor = await proveedorRepository.GetByIdAsync(funko.ProveedorId);
        if (proveedor is null) {
            logger.LogWarning("El proveedor {ProveedorId} del funko {Id} no existe", funko.ProveedorId, id);
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.ProveedorNotFound(funko.ProveedorId));
        }

        var respuesta = funko.ToResponseDto(proveedor);
        await cache.SetAsync(cacheKey, respuesta);
        return Result.Success<ResponseFunkoDto, DomainError>(respuesta);
    }

    public async Task<Result<ResponseFunkoDto, DomainError>> AddAsync(CreateFunkoDto dto) {
        logger.LogInformation("Creando funko {Nombre}", dto.Nombre);

        // validar
        var validationResult = await createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) {
            var errorMsg = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.Validation(errorMsg));
        }

        // la categoría y el proveedor tienen que existir
        var categoria = await categoriaRepository.GetByIdAsync(dto.CategoriaId);
        if (categoria is null) {
            logger.LogWarning("Categoría {CategoriaId} no encontrada al crear funko", dto.CategoriaId);
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.CategoriaNotFound(dto.CategoriaId));
        }

        var proveedor = await proveedorRepository.GetByIdAsync(dto.ProveedorId);
        if (proveedor is null or { IsDeleted: true }) {
            logger.LogWarning("Proveedor {ProveedorId} no encontrado al crear funko", dto.ProveedorId);
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.ProveedorNotFound(dto.ProveedorId));
        }

        var creado = await funkoRepository.AddAsync(dto.ToEntity());

        // se vuelve a leer para traer la categoría (Include) sin tocar entidades que EF está siguiendo
        var completo = await funkoRepository.GetByIdAsync(creado.Id);

        logger.LogInformation("Funko {Id} creado", creado.Id);
        return Result.Success<ResponseFunkoDto, DomainError>(completo!.ToResponseDto(proveedor));
    }

    public async Task<Result<ResponseFunkoDto, DomainError>> UpdateAsync(long id, UpdateFunkoDto dto) {
        logger.LogInformation("Actualizando funko {Id}", id);

        // validar
        var validationResult = await updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) {
            var errorMsg = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.Validation(errorMsg));
        }

        var existente = await funkoRepository.GetByIdAsync(id);
        if (existente is null) {
            logger.LogWarning("Funko {Id} no encontrado al actualizar", id);
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.NotFound(id.ToString()));
        }

        var categoria = await categoriaRepository.GetByIdAsync(dto.CategoriaId);
        if (categoria is null) {
            logger.LogWarning("Categoría {CategoriaId} no encontrada al actualizar funko {Id}", dto.CategoriaId, id);
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.CategoriaNotFound(dto.CategoriaId));
        }

        var proveedor = await proveedorRepository.GetByIdAsync(dto.ProveedorId);
        if (proveedor is null or { IsDeleted: true }) {
            logger.LogWarning("Proveedor {ProveedorId} no encontrado al actualizar funko {Id}", dto.ProveedorId, id);
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.ProveedorNotFound(dto.ProveedorId));
        }

        dto.UpdateEntity(existente);
        var actualizado = await funkoRepository.UpdateAsync(existente);
        if (actualizado is null) {
            // se borró entre la lectura y la escritura
            return Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.NotFound(id.ToString()));
        }

        // la caché ya no es válida
        await cache.RemoveAsync(CacheKey(id));

        var completo = await funkoRepository.GetByIdAsync(id);

        logger.LogInformation("Funko {Id} actualizado", id);
        return Result.Success<ResponseFunkoDto, DomainError>(completo!.ToResponseDto(proveedor));
    }

    public async Task<UnitResult<DomainError>> DeleteAsync(long id) {
        logger.LogInformation("Borrando funko {Id}", id);

        var borrado = await funkoRepository.DeleteAsync(id);
        if (!borrado) {
            logger.LogWarning("Funko {Id} no encontrado al borrar", id);
            return UnitResult.Failure<DomainError>(ModelErrors.NotFound(id.ToString()));
        }

        await cache.RemoveAsync(CacheKey(id));

        logger.LogInformation("Funko {Id} borrado", id);
        return UnitResult.Success<DomainError>();
    }
}