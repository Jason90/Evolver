namespace Evolver.Core.Entities;

public sealed class SystemParameter : BaseEntity
{
    public string Name { get; set; } = "";
    public string ParamKey { get; set; } = "";
    public string ParamValue { get; set; } = "";
    public bool IsSystemBuiltIn { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}
