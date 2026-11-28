using System.Linq.Expressions;
using AR.Core.Persistence.Context;
using AR.Domain;
using AR.NetExt.MayBe;
using Microsoft.EntityFrameworkCore;

namespace AR.Core.Persistence.Repositories;

public interface IRepository<T> where T : Entity
{
    ValueTask<MayBe<T>> GetAsync(object id);
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? predicate = null, int limit = int.MaxValue, int offset = 0);
    IQueryable<T> GetBy(Expression<Func<T, bool>>? predicate = null);
    IQueryable<T> GetAll();
    void Remove(T entity);
    void RemoveRange(IReadOnlyList<T> entities);
    Task AddRangeAsync(IReadOnlyList<T> entities);
    Task AddAsync(T entity);
    void UpdateRange(IReadOnlyList<T> entities);
}

internal sealed class Repository<T> : IRepository<T> where T : Entity
{
    private readonly DbSet<T> _dbSet;

    public Repository(DbDomainContext context) => _dbSet = context.Set<T>();

    public async ValueTask<MayBe<T>> GetAsync(object id) => new(await _dbSet.FindAsync(id));

    public IQueryable<T> GetAll() => _dbSet;

    public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? predicate = null, int limit = int.MaxValue, int offset = 0)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.Skip(offset).Take(limit).ToListAsync();
    }

    public IQueryable<T> GetBy(Expression<Func<T, bool>>? predicate = null)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return query;
    }

    public void Remove(T entity) => _dbSet.Remove(entity);

    public void RemoveRange(IReadOnlyList<T> entities) => _dbSet.RemoveRange(entities);

    public Task AddRangeAsync(IReadOnlyList<T> entities) => _dbSet.AddRangeAsync(entities);

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void UpdateRange(IReadOnlyList<T> entities) => _dbSet.UpdateRange(entities);
}