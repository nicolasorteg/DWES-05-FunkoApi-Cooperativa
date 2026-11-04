using FunkoApi.Models;

namespace FunkoApi.Repositories.Common;

public interface ICrudRepository<TEntity, in TKey> where TEntity : class {
    Task<IReadOnlyList<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<TEntity> AddAsync(TEntity entity);
    Task<TEntity?> UpdateAsync(TEntity entity);
    Task<bool> DeleteAsync(TKey id);
}