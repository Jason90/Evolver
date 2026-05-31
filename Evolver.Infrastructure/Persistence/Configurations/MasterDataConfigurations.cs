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

internal sealed class EnumTypeConfigConfiguration : IEntityTypeConfiguration<EnumTypeConfig>
{
    public void Configure(EntityTypeBuilder<EnumTypeConfig> builder)
    {
        builder.ToTable("EnumTypes");
        builder.Property(x => x.EnumTypeCode).HasColumnName("EnumTypeCode").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("Name").HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("Description").HasMaxLength(300);
        builder.Property(x => x.IsActive).HasColumnName("IsActive");
        builder.Property(x => x.UpdateTime).HasColumnName("UpdateTime");
        builder.Property(x => x.UpdateBy).HasColumnName("UpdateBy");

        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.EnumTypeCode }).IsUnique();
        builder.HasMany(x => x.Values)
            .WithOne(v => v.EnumType)
            .HasForeignKey(v => new { v.TenantId, v.OrgId, v.EnumTypeCode })
            .HasPrincipalKey(x => new { x.TenantId, x.OrgId, x.EnumTypeCode })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EnumValueConfigConfiguration : IEntityTypeConfiguration<EnumValueConfig>
{
    public void Configure(EntityTypeBuilder<EnumValueConfig> builder)
    {
        builder.ToTable("EnumValues");
        builder.Property(x => x.EnumTypeCode).HasColumnName("EnumTypeCode").HasMaxLength(50);
        builder.Property(x => x.EnumValueCode).HasColumnName("EnumValueCode").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("Name").HasMaxLength(100);
        builder.Property(x => x.SortNo).HasColumnName("SortNo");
        builder.Property(x => x.IsDefault).HasColumnName("IsDefault");
        builder.Property(x => x.Description).HasColumnName("Description").HasMaxLength(300);
        builder.Property(x => x.IsActive).HasColumnName("IsActive");
        builder.Property(x => x.UpdateTime).HasColumnName("UpdateTime");
        builder.Property(x => x.UpdateBy).HasColumnName("UpdateBy");

        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.EnumTypeCode, x.EnumValueCode }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.EnumTypeCode, x.SortNo });
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

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.Code }).IsUnique();
    }
}

internal sealed class SystemParameterConfiguration : IEntityTypeConfiguration<SystemParameter>
{
    public void Configure(EntityTypeBuilder<SystemParameter> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.ParamKey }).IsUnique();
    }
}
