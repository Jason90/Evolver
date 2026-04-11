using System.Linq.Expressions;
using Evolver.Core.Entities;
using Evolver.Core.Pagination;

namespace Evolver.Core.Repositories;

/// <summary>
/// 通用仓储（单表）。依赖持久化上下文的<strong>全局查询过滤</strong>实现多租户与软删除；
/// <see cref="Query"/> 默认应用上述过滤；需要跨租户或包含已删数据时将参数 <c>ignoreQueryFilters</c> 设为 <c>true</c>。
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// 可组合查询。默认：当前 <c>TenantId</c> + <c>OrgId</c> + 未删除；可选忽略全局过滤、只读跟踪。
    /// </summary>
    IQueryable<TEntity> Query(bool ignoreQueryFilters = false, bool asNoTracking = false);

    Task<TEntity?> GetByIdAsync(
        long id,
        bool ignoreQueryFilters = false,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    void AddRange(IEnumerable<TEntity> entities);

    void Update(TEntity entity);

    /// <summary>软删除：仅设置 <see cref="BaseEntity.IsDeleted"/>，审计字段在 <c>SaveChanges</c> 中写入。</summary>
    void SoftDelete(TEntity entity);

    /// <summary>按主键软删除（仅在当前租户/组织且未删除时能加载到行）。</summary>
    Task<bool> SoftDeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件分页（无固定排序）。需要稳定顺序时请先 <c>OrderBy</c>，或使用 Infrastructure 中的 <c>QueryablePagedExtensions.ToPagedAsync</c> 对任意 <c>IQueryable</c> 分页。
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
