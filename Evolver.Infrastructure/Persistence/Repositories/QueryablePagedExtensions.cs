using Evolver.Core.Entities;
using Evolver.Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Infrastructure.Persistence.Repositories;

/// <summary>
/// 对任意 <see cref="IQueryable{T}"/>（通常来自 <see cref="IRepository{TEntity}.Query"/>）做分页。
/// </summary>
public static class QueryablePagedExtensions
{
    public static async Task<PagedResult<TEntity>> ToPagedAsync<TEntity>(
        this IQueryable<TEntity> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) where TEntity : BaseEntity
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TEntity>(items, total, pageNumber, pageSize);
    }
}
