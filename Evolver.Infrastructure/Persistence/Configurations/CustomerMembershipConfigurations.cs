using Evolver.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Evolver.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasOne(x => x.CustomerCategory)
            .WithMany()
            .HasForeignKey(x => x.CustomerCategoryRefId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.Code }).IsUnique();
    }
}

internal sealed class CustomerCategoryConfiguration : IEntityTypeConfiguration<CustomerCategory>
{
    public void Configure(EntityTypeBuilder<CustomerCategory> builder)
    {
        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.CategoryCode }).IsUnique();
    }
}

internal sealed class MembershipAccountConfiguration : IEntityTypeConfiguration<MembershipAccount>
{
    public void Configure(EntityTypeBuilder<MembershipAccount> builder)
    {
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.OrgId, x.CustomerId }).IsUnique();
    }
}

internal sealed class MembershipPointsLedgerConfiguration : IEntityTypeConfiguration<MembershipPointsLedger>
{
    public void Configure(EntityTypeBuilder<MembershipPointsLedger> builder)
    {
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalesOrder)
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
