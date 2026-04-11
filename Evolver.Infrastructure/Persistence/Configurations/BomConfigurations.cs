using Evolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evolver.Infrastructure.Persistence.Configurations;

internal sealed class BomHeaderConfiguration : IEntityTypeConfiguration<BomHeader>
{
    public void Configure(EntityTypeBuilder<BomHeader> builder)
    {
        builder.HasOne(x => x.FinishedProduct)
            .WithMany()
            .HasForeignKey(x => x.FinishedProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BomLineConfiguration : IEntityTypeConfiguration<BomLine>
{
    public void Configure(EntityTypeBuilder<BomLine> builder)
    {
        builder.HasOne(x => x.BomHeader)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.BomHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ComponentProduct)
            .WithMany()
            .HasForeignKey(x => x.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BomHeaderId, x.ComponentProductId }).IsUnique();
    }
}
