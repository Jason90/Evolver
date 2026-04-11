using Evolver.Application.Import;
using Evolver.Application.MenuIntelligence;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Core.Repositories;
using Evolver.Infrastructure.Persistence;
using Evolver.Infrastructure.Persistence.Repositories;
using Evolver.Web.Extensions;
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

var app = builder.Build();

app.UseEvolverApiPipeline();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

    async Task SeedAdminAsync()
    {
        const string adminUser = "admin";
        const string adminRole = "Admin";
        var adminPassword = builder.Configuration["Admin:Password"] ?? "admin123";

        if (!await roleMgr.RoleExistsAsync(adminRole))
            await roleMgr.CreateAsync(new AppRole { Name = adminRole });

        var existing = await userMgr.FindByNameAsync(adminUser);
        if (existing is not null)
            return;

        var u = new AppUser
        {
            UserName = adminUser,
            Email = "admin@local",
            EmailConfirmed = true,
            TenantId = 1,
            OrgId = 1
        };
        var res = await userMgr.CreateAsync(u, adminPassword);
        if (res.Succeeded)
            await userMgr.AddToRoleAsync(u, adminRole);
    }

    await SeedAdminAsync();

    async Task EnsurePermissionAsync(string code, string name, PermissionType type, string? resource = null, long? parentId = null)
    {
        if (await db.Permissions.AnyAsync(p => p.Code == code))
            return;

        db.Permissions.Add(new Permission
        {
            TenantId = 1,
            OrgId = 1,
            Code = code,
            Name = name,
            Type = type,
            Resource = resource,
            ParentId = parentId
        });
        await db.SaveChangesAsync();
    }

    await EnsurePermissionAsync("permissions.read", "Permissions.Read", PermissionType.Api, "GET /api/permissions/tree");
    await EnsurePermissionAsync("permissions.write", "Permissions.Write", PermissionType.Api, "POST /api/permissions");
    await EnsurePermissionAsync("roles.read", "Roles.Read", PermissionType.Api, "GET /api/roles");
    await EnsurePermissionAsync("roles.write", "Roles.Write", PermissionType.Api, "POST /api/roles; POST /api/roles/{id}/permissions");

    await EnsurePermissionAsync("users.read", "Users.Read", PermissionType.Api, "GET /api/users");
    await EnsurePermissionAsync("users.write", "Users.Write", PermissionType.Api, "POST/PUT/DELETE /api/users");
    await EnsurePermissionAsync("organizations.read", "Organizations.Read", PermissionType.Api, "GET /api/organizations");
    await EnsurePermissionAsync("organizations.write", "Organizations.Write", PermissionType.Api, "POST/PUT/DELETE /api/organizations");
    await EnsurePermissionAsync("dictionary.read", "Dictionary.Read", PermissionType.Api, "GET /api/data-dictionary");
    await EnsurePermissionAsync("dictionary.write", "Dictionary.Write", PermissionType.Api, "POST/DELETE /api/data-dictionary");

    if (!await db.Organizations.AnyAsync())
    {
        db.Organizations.Add(new Organization
        {
            TenantId = 1,
            OrgId = 1,
            ParentId = null,
            Name = "总部",
            OrgType = "Headquarters",
            IsDeleted = false
        });
        await db.SaveChangesAsync();
    }

    if (!await db.Products.AnyAsync())
    {
        var now = DateTime.UtcNow;
        db.Products.AddRange(
            new Product
            {
                TenantId = 1,
                OrgId = 1,
                Code = "SKU-001",
                Name = "示例商品 A",
                UnitPrice = 29.99m,
                UnitCost = 12.50m,
                CreateTime = now,
                IsDeleted = false
            },
            new Product
            {
                TenantId = 1,
                OrgId = 1,
                Code = "SKU-002",
                Name = "示例商品 B",
                UnitPrice = 15.00m,
                UnitCost = null,
                CreateTime = now,
                IsDeleted = false
            });
        await db.SaveChangesAsync();
    }
}

app.Run();

public partial class Program;
