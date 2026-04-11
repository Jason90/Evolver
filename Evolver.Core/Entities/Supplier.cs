namespace Evolver.Core.Entities;

public sealed class Supplier : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? SupplierType { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
}
