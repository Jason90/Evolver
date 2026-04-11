using Evolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evolver.Infrastructure.Persistence.Configurations;

internal sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

internal sealed class SalesEntryConfiguration : IEntityTypeConfiguration<SalesEntry>
{
    public void Configure(EntityTypeBuilder<SalesEntry> builder)
    {
        builder.HasOne(x => x.Market)
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.MarketId, x.Date, x.ProductId });
    }
}

internal sealed class MenuIntelligenceRecordConfiguration : IEntityTypeConfiguration<MenuIntelligenceRecord>
{
    public void Configure(EntityTypeBuilder<MenuIntelligenceRecord> builder)
    {
        builder.HasOne(x => x.Market)
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.MarketId, x.AsOfDate, x.Rank });
    }
}

internal sealed class MarketInventorySnapshotConfiguration : IEntityTypeConfiguration<MarketInventorySnapshot>
{
    public void Configure(EntityTypeBuilder<MarketInventorySnapshot> builder)
    {
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Market)
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.MarketId, x.ProductId, x.AsOfDate }).IsUnique();
    }
}
