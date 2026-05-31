using Evolver.Core.Entities;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace Evolver.Infrastructure.Persistence.Configurations;



internal sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>

{

    public void Configure(EntityTypeBuilder<AppRole> builder)

    {

        builder.Property(r => r.IsActive).HasDefaultValue(true);



        builder.HasIndex(r => r.NormalizedName)

            .HasDatabaseName("RoleNameIndex")

            .IsUnique(false);



        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })

            .IsUnique()

            .HasFilter("IsActive = 1");

    }

}


