using Evolver.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evolver.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core tools (e.g. dotnet ef migrations add).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=Evolver.dev.db", x => x.MigrationsAssembly("Evolver.Infrastructure"));

        var tenant = new TenantContext();
        return new AppDbContext(optionsBuilder.Options, tenant);
    }
}
