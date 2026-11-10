using CSharpFunctionalExtensions;
using FunkoApi.Errors.Common;

namespace FunkoApi.Services.Common;

public interface ICrudService<TResponseDto, in TCreateDto, in TUpdateDto, in TKey> where TResponseDto : class {
    Task<Result<IReadOnlyList<TResponseDto>, DomainError>>GetAllPagedAsync(int page = 1, int pageSize = 10);
    Task<Result<TResponseDto, DomainError>> GetByIdAsync(TKey id);
    Task<Result<TResponseDto, DomainError>> AddAsync(TCreateDto entity);
    Task<Result<TResponseDto, DomainError>> UpdateAsync(TKey id, TUpdateDto entity);
    Task<UnitResult<DomainError>> DeleteAsync(TKey id);
}