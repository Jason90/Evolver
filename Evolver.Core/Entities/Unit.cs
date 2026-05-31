namespace Evolver.Core.Entities;

public sealed class Unit : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>单位停用后保留历史数据，不再给其他业务模块使用。</summary>
    public bool IsActive { get; set; } = true;
}
