using Evolver.Core.Entities;

namespace Evolver.Core.Tests;

public sealed class ProductEntityTests
{
    [Fact]
    public void Product_Inherits_BaseEntity_Audit_And_Tenant_Fields()
    {
        var p = new Product
        {
            TenantId = 2,
            OrgId = 3,
            Code = "T",
            Name = "Test",
            UnitPrice = 1m
        };

        Assert.Equal(2, p.TenantId);
        Assert.Equal(3, p.OrgId);
        Assert.Null(p.UpdateTime);
    }
}
