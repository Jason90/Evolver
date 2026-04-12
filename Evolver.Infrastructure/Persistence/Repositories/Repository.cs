using Evolver.Core.Entities;
using Evolver.Core.Pagination;
using Evolver.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Evolver.Infrastructure.Persistence.Repositories;

public sealed class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly AppDbContext _db;

    public Repository(AppDbContext db) =>
        _db = db;

    public IQueryable<TEntity> Query(bool ignoreQueryFilters = false, bool asNoTracking = false)
    {
        var q = _db.Set<TEntity>().AsQueryable();
        if (ignoreQueryFilters)
            q = q.IgnoreQueryFilters();
        if (asNoTracking)
            q = q.AsNoTracking();
        return q;
    }

    public async Task<TEntity?> GetByIdAsync(
        long id,
        bool ignoreQueryFilters = false,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var q = Query(ignoreQueryFilters, asNoTracking);
        return await q.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default) =>
        Query(asNoTracking: true).AnyAsync(e => e.Id == id, cancellationToken);

    public void Add(TEntity entity) =>
        _db.Set<TEntity>().Add(entity);

    public void AddRange(IEnumerable<TEntity> entities) =>
        _db.Set<TEntity>().AddRange(entities);

    public void Update(TEntity entity) =>
        _db.Set<TEntity>().Update(entity);

    public void Delete(TEntity entity) =>
        _db.Set<TEntity>().Remove(entity);

    public async Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, ignoreQueryFilters: false, asNoTracking: false, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            return false;
        Delete(entity);
        return true;
    }

    public async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        var q = Query(asNoTracking: true);
        if (predicate is not null)
            q = q.Where(predicate);

        return await q.ToPagedAsync(pageNumber, pageSize, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
