using Evolver.Application.Import;
using Evolver.Application.MenuIntelligence;
using Evolver.Core.Entities;
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
builder.Services.AddScoped<MarketSpreadsheetService>();
builder.Services.AddScoped<SystemParameterSpreadsheetService>();
builder.Services.AddScoped<UserSpreadsheetService>();
builder.Services.AddScoped<UnitSpreadsheetService>();
builder.Services.AddScoped<SupplierSpreadsheetService>();
builder.Services.AddScoped<CustomerCategorySpreadsheetService>();
builder.Services.AddScoped<CustomerSpreadsheetService>();
builder.Services.AddScoped<ProductCategorySpreadsheetService>();
builder.Services.AddScoped<ProductSpreadsheetService>();
builder.Services.AddScoped<RoleSpreadsheetService>();
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddScoped<TenantSpreadsheetService>();

var app = builder.Build();

app.UseEvolverApiPipeline();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    async Task EnsureSqliteColumnAsync(
        string tableName,
        string columnName,
        string columnSql,
        string? backfillSql = null,
        CancellationToken ct = default)
    {
        if (!db.Database.IsSqlite())
            return;

        await using var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = $"PRAGMA table_info('{tableName}')";
        var exists = false;
        await using (var reader = await checkCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (!exists)
        {
            await using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnSql}";
            await alterCmd.ExecuteNonQueryAsync(ct);
        }

        if (!string.IsNullOrWhiteSpace(backfillSql))
        {
            await using var backfillCmd = conn.CreateCommand();
            backfillCmd.CommandText = backfillSql;
            await backfillCmd.ExecuteNonQueryAsync(ct);
        }
    }

    async Task EnsureSqliteRenameColumnAsync(string tableName, string oldColumnName, string newColumnName, CancellationToken ct = default)
    {
        if (!db.Database.IsSqlite())
            return;

        await using var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = $"PRAGMA table_info('{tableName}')";

        var hasOld = false;
        var hasNew = false;
        await using (var reader = await checkCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var col = reader["name"]?.ToString();
                if (string.Equals(col, oldColumnName, StringComparison.OrdinalIgnoreCase))
                    hasOld = true;
                if (string.Equals(col, newColumnName, StringComparison.OrdinalIgnoreCase))
                    hasNew = true;
            }
        }

        if (!hasOld || hasNew)
            return;

        await using var renameCmd = conn.CreateCommand();
        renameCmd.CommandText = $"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{oldColumnName}\" TO \"{newColumnName}\"";
        await renameCmd.ExecuteNonQueryAsync(ct);
    }

    async Task EnsureSqliteActiveColumnFromDeletedAsync(string tableName, CancellationToken ct = default)
    {
        if (!db.Database.IsSqlite())
            return;

        await using var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = $"PRAGMA table_info('{tableName}')";

        var hasIsActive = false;
        var hasIsDeleted = false;
        await using (var reader = await checkCmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var col = reader["name"]?.ToString();
                if (string.Equals(col, "IsActive", StringComparison.OrdinalIgnoreCase))
                    hasIsActive = true;
                if (string.Equals(col, "IsDeleted", StringComparison.OrdinalIgnoreCase))
                    hasIsDeleted = true;
            }
        }

        if (!hasIsDeleted)
            return;

        if (!hasIsActive)
        {
            await using var addCmd = conn.CreateCommand();
            addCmd.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"IsActive\" INTEGER NOT NULL DEFAULT 1";
            await addCmd.ExecuteNonQueryAsync(ct);
        }

        await using var backfillCmd = conn.CreateCommand();
        backfillCmd.CommandText = $"UPDATE \"{tableName}\" SET \"IsActive\" = CASE WHEN \"IsDeleted\" = 1 THEN 0 ELSE 1 END";
        await backfillCmd.ExecuteNonQueryAsync(ct);
    }

    await EnsureSqliteColumnAsync("Products", "IsActive", "INTEGER NOT NULL DEFAULT 1");
    await EnsureSqliteRenameColumnAsync("CustomerCategories", "CategoryId", "CategoryCode");
    await EnsureSqliteColumnAsync("Customers", "CustomerCategoryRefId", "INTEGER NULL");
    await EnsureSqliteColumnAsync("Customers", "Gender", "TEXT NULL");
    await EnsureSqliteColumnAsync("Customers", "Birthday", "TEXT NULL");
    await EnsureSqliteColumnAsync("Customers", "JobTitle", "TEXT NULL");
    await EnsureSqliteColumnAsync("Customers", "Remark", "TEXT NULL");
    await EnsureSqliteColumnAsync("AspNetUsers", "Remark", "TEXT NULL");
    await EnsureSqliteColumnAsync("Customers", "IsActive", "INTEGER NOT NULL DEFAULT 1");
    await EnsureSqliteColumnAsync("Organizations", "IsActive", "INTEGER NOT NULL DEFAULT 1");
    // SQLite ALTER TABLE 不支持 DEFAULT CURRENT_TIMESTAMP，先加可空列再回填。
    await EnsureSqliteColumnAsync(
        "Tenants",
        "CreateTime",
        "TEXT NULL",
        "UPDATE \"Tenants\" SET \"CreateTime\" = datetime('now') WHERE \"CreateTime\" IS NULL");
    await EnsureSqliteColumnAsync("Tenants", "ExpireAt", "TEXT NULL");
    await EnsureSqliteColumnAsync("Tenants", "Remark", "TEXT NULL");
    await EnsureSqliteActiveColumnFromDeletedAsync("AspNetRoles");
    await EnsureSqliteActiveColumnFromDeletedAsync("Tenants");

    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

    async Task EnsurePlatformTenantRowAsync()
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == 1))
            return;

        db.Tenants.Add(new Tenant
        {
            Id = 1,
            TenantId = 1,
            OrgId = 0,
            Name = "Default",
            IsActive = true,
            CreateTime = DateTime.UtcNow
        });
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

        var normalized = userMgr.NormalizeName(adminUser)!;
        var existing = await userMgr.Users.FirstOrDefaultAsync(u => u.TenantId == 1 && u.NormalizedUserName == normalized);
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

    async Task SeedDefaultProductsAsync()
    {
        var defaults = new (string Code, string Name)[]
        {
            ("BB", "Brekkie Buns"),
            ("BR", "BBQ Ribs"),
            ("WS", "Wagyu Beef Sliders"),
            ("SM", "Smokies"),
            ("PP", "Pulled Pork"),
            ("WD", "Wedges"),
            ("OR", "Onion Rings"),
            ("CP", "Chicken Lollies"),
            ("DR", "Drinks"),
            ("US", "Up-sell")
        };

        var existingCodes = await db.Products
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == 1 && x.OrgId == 1)
            .Select(x => x.Code)
            .ToListAsync();

        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var created = false;

        foreach (var (code, name) in defaults)
        {
            if (existingSet.Contains(code))
                continue;

            db.Products.Add(new Product
            {
                TenantId = 1,
                OrgId = 1,
                Code = code,
                Name = name,
                UnitPrice = 0m,
                SuggestedPrice = 0m,
                UnitCost = 0m,
                IsActive = true
            });
            created = true;
        }

        if (created)
            await db.SaveChangesAsync();
    }

    async Task SeedDefaultUnitsAsync()
    {
        var defaults = new[]
        {
            ("Kg", "Kg"),
            ("Pieces", "Pieces"),
            ("Packs", "Packs")
        };

        var existingCodes = await db.Units
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == 1 && x.OrgId == 1)
            .Select(x => x.Code)
            .ToListAsync();

        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var created = false;

        foreach (var (code, name) in defaults)
        {
            if (existingSet.Contains(code))
                continue;

            db.Units.Add(new Unit
            {
                TenantId = 1,
                OrgId = 1,
                Code = code,
                Name = name,
                IsActive = true
            });
            created = true;
        }

        if (created)
            await db.SaveChangesAsync();
    }

    async Task SeedDefaultMarketsAsync()
    {
        var defaults = new[]
        {
            "Britomart Market",
            "EventFinda Market",
            "Grafton Market",
            "Victoria Park Market",
            "Long Bay Market"
        };

        var existingNames = await db.Markets
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == 1 && x.OrgId == 1)
            .Select(x => x.Name)
            .ToListAsync();

        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var created = false;

        foreach (var name in defaults)
        {
            if (existingSet.Contains(name))
                continue;

            db.Markets.Add(new Market
            {
                TenantId = 1,
                OrgId = 1,
                Name = name,
                IsActive = true
            });
            created = true;
        }

        if (created)
            await db.SaveChangesAsync();
    }

    await SeedDefaultProductsAsync();
    await SeedDefaultUnitsAsync();
    await SeedDefaultMarketsAsync();

    async Task SeedEnumConfigAsync()
    {
        var typeSeeds = new (string Code, string Name, string? Desc)[]
        {
            ("ProductType", "商品类型", "原材料/半成品/成品/组合品/副产品/服务项"),
            ("ProductionOrderStatus", "生产工单状态", "计划到完工的状态流转"),
            ("InventoryTransactionType", "库存流水类型", "入库/出库/调整等"),
            ("BomStatus", "BOM状态", "BOM版本状态"),
        };

        foreach (var (code, name, desc) in typeSeeds)
        {
            var t = await db.EnumTypeConfigs.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == 1 && x.OrgId == 1 && x.EnumTypeCode == code);
            if (t is null)
            {
                db.EnumTypeConfigs.Add(new EnumTypeConfig
                {
                    TenantId = 1,
                    OrgId = 1,
                    EnumTypeCode = code,
                    Name = name,
                    Description = desc,
                    IsActive = true
                });
            }
            else
            {
                t.Name = name;
                t.Description = desc;
                t.IsActive = true;
            }
        }

        await db.SaveChangesAsync();

        var valueSeeds = new (string TypeCode, string ValueCode, string Name, int SortNo, bool IsDefault, string? Desc)[]
        {
            ("ProductType","Raw","原材料",10,false,null),
            ("ProductType","Semi","半成品",20,false,null),
            ("ProductType","Finished","成品",30,true,null),
            ("ProductType","Kit","组合品",40,false,"套餐/套件"),
            ("ProductType","ByProduct","副产品",50,false,"副产物"),
            ("ProductType","Service","服务项",60,false,"不入库的服务项目"),

            ("ProductionOrderStatus","Planned","已计划",10,true,null),
            ("ProductionOrderStatus","Released","已下达",20,false,null),
            ("ProductionOrderStatus","InProgress","生产中",30,false,null),
            ("ProductionOrderStatus","Completed","已完工",40,false,null),
            ("ProductionOrderStatus","Closed","已关闭",50,false,null),
            ("ProductionOrderStatus","Cancelled","已取消",60,false,null),

            ("InventoryTransactionType","Issue","领料出库",10,false,null),
            ("InventoryTransactionType","Receipt","产成品入库",20,false,null),
            ("InventoryTransactionType","Return","退料入库",30,false,null),
            ("InventoryTransactionType","Scrap","报废",40,false,null),
            ("InventoryTransactionType","Adjust","库存调整",50,true,null),
            ("InventoryTransactionType","TransferOut","调拨出库",60,false,null),
            ("InventoryTransactionType","TransferIn","调拨入库",70,false,null),

            ("BomStatus","Draft","草稿",10,true,null),
            ("BomStatus","Approved","已生效",20,false,null),
            ("BomStatus","Obsolete","已作废",30,false,null),
        };

        foreach (var (typeCode, valueCode, name, sortNo, isDefault, desc) in valueSeeds)
        {
            var v = await db.EnumValueConfigs.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == 1 && x.OrgId == 1 && x.EnumTypeCode == typeCode && x.EnumValueCode == valueCode);
            if (v is null)
            {
                db.EnumValueConfigs.Add(new EnumValueConfig
                {
                    TenantId = 1,
                    OrgId = 1,
                    EnumTypeCode = typeCode,
                    EnumValueCode = valueCode,
                    Name = name,
                    SortNo = sortNo,
                    IsDefault = isDefault,
                    Description = desc,
                    IsActive = true
                });
            }
            else
            {
                v.Name = name;
                v.SortNo = sortNo;
                v.IsDefault = isDefault;
                v.Description = desc;
                v.IsActive = true;
            }
        }

        await db.SaveChangesAsync();
    }

    await SeedEnumConfigAsync();
}

app.Run();

public partial class Program;
