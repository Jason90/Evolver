using Evolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evolver.Infrastructure.Persistence.Configurations;

internal sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.HasOne(x => x.OutputProduct)
            .WithMany()
            .HasForeignKey(x => x.OutputProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceSalesOrder)
            .WithMany()
            .HasForeignKey(x => x.SourceSalesOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.OrderNo }).IsUnique();
    }
}

internal sealed class ProductionOrderMaterialLineConfiguration : IEntityTypeConfiguration<ProductionOrderMaterialLine>
{
    public void Configure(EntityTypeBuilder<ProductionOrderMaterialLine> builder)
    {
        builder.HasOne(x => x.ProductionOrder)
            .WithMany(x => x.MaterialLines)
            .HasForeignKey(x => x.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MaterialProduct)
            .WithMany()
            .HasForeignKey(x => x.MaterialProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductionWasteRecordConfiguration : IEntityTypeConfiguration<ProductionWasteRecord>
{
    public void Configure(EntityTypeBuilder<ProductionWasteRecord> builder)
    {
        builder.HasOne(x => x.ProductionOrder)
            .WithMany(x => x.WasteRecords)
            .HasForeignKey(x => x.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
