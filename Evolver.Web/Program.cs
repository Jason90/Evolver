using Evolver.Application.Import;
using Evolver.Application.MenuIntelligence;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Core.Repositories;
using Evolver.Infrastructure.Persistence;
using Evolver.Infrastructure.Persistence.Repositories;
using Evolver.Web.Extensions;
using Evolver.Web.Options;
using Evolver.Web.Seeding;
using Evolver.Web.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.MSSqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext();

    var cs = ctx.Configuration.GetConnectionString("Default");
    if (!string.IsNullOrWhiteSpace(cs))
    {
        var t = cs.TrimStart();
        var isSqlite = t.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase);
        if (!isSqlite)
        {
            cfg.WriteTo.MSSqlServer(
                connectionString: cs,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    TableName = "Logs",
                    AutoCreateSqlTable = true
                });
        }
    }
});

builder.Services.Configure<FormOptions>(o => { o.MultipartBodyLengthLimit = 20 * 1024 * 1024; });
builder.Services.Configure<PlatformOptions>(builder.Configuration.GetSection(PlatformOptions.SectionName));
builder.Services.AddEvolverControllersWithUnifiedApi();
builder.Services.AddEvolverSwagger();
builder.Services.AddEvolverDevCors();
builder.Services.AddEvolverTenantContext();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing ConnectionStrings:Default");

    if (cs.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
        cs.TrimStart().StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(cs, x => x.MigrationsAssembly("Evolver.Infrastructure"));
    }
    else
    {
        options.UseSqlServer(cs, x => x.MigrationsAssembly("Evolver.Infrastructure"));
    }
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddEvolverJwtAuthentication(builder.Configuration);
builder.Services.AddEvolverPermissionAuthorization();
builder.Services.AddEvolverIdentity();

builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<MenuIntelligenceService>();
builder.Services.AddScoped<UserSpreadsheetService>();
builder.Services.AddScoped<RoleSpreadsheetService>();
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddScoped<TenantSpreadsheetService>();

var app = builder.Build();

app.UseEvolverApiPipeline();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

    async Task EnsurePlatformTenantRowAsync()
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == 1))
            return;

        db.Tenants.Add(new Tenant { Id = 1, TenantId = 1, OrgId = 0, Name = "Default" });
        await db.SaveChangesAsync();
    }

    await EnsurePlatformTenantRowAsync();

    async Task SeedAdminAsync()
    {
        const string adminUser = "admin";
        const string adminRole = "Admin";
        var adminPassword = builder.Configuration["Admin:Password"] ?? "admin123";

        if (!await roleMgr.Roles.AnyAsync(r => r.Name == adminRole && r.TenantId == 1))
            await roleMgr.CreateAsync(new AppRole { Name = adminRole, TenantId = 1, OrgId = 1 });

        var normalized = userMgr.NormalizeName(adminUser);
        var existing = normalized is null
            ? null
            : await userMgr.Users.FirstOrDefaultAsync(u => u.TenantId == 1 && u.NormalizedUserName == normalized);
        if (existing is not null)
            return;

        var u = new AppUser
        {
            UserName = adminUser,
            Email = "admin@local",
            EmailConfirmed = true,
            TenantId = 1,
            OrgId = 1,
            IsActive = true
        };
        var res = await userMgr.CreateAsync(u, adminPassword);
        if (!res.Succeeded)
            return;

        var role = await roleMgr.Roles.FirstAsync(r => r.TenantId == 1 && r.Name == adminRole);
        db.UserRoles.Add(new IdentityUserRole<long> { UserId = u.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
    }

    await SeedAdminAsync();

    await NavigationMenuPermissionSeeder.EnsureCatalogAsync(db, tenantId: 1, orgId: 1);
    await LegacySystemApiToNavPermissionMigration.MigrateAsync(db);

    if (!await db.Organizations.AnyAsync())
    {
        db.Organizations.Add(new Organization
        {
            TenantId = 1,
            OrgId = 1,
            ParentId = null,
            Name = "总部",
            OrgType = "Headquarters"
        });
        await db.SaveChangesAsync();
    }

    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product
            {
                TenantId = 1,
                OrgId = 1,
                Code = "SKU-001",
                Name = "示例商品 A",
                UnitPrice = 29.99m,
                UnitCost = 12.50m
            },
            new Product
            {
                TenantId = 1,
                OrgId = 1,
                Code = "SKU-002",
                Name = "示例商品 B",
                UnitPrice = 15.00m,
                UnitCost = null
            });
        await db.SaveChangesAsync();
    }
}

app.Run();

public partial class Program;
