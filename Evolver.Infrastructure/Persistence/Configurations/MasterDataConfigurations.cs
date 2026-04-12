using Evolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evolver.Infrastructure.Persistence.Configurations;

internal sealed class DataDictionaryItemConfiguration : IEntityTypeConfiguration<DataDictionaryItem>
{
    public void Configure(EntityTypeBuilder<DataDictionaryItem> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.CategoryCode, x.ItemCode }).IsUnique();
    }
}

internal sealed class DataDictionaryTypeConfiguration : IEntityTypeConfiguration<DataDictionaryType>
{
    public void Configure(EntityTypeBuilder<DataDictionaryType> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.TypeCode }).IsUnique();
    }
}

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.Code }).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.ProductCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}
