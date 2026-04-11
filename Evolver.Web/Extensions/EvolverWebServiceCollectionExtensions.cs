using System.Text;
using Evolver.Application.Security;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Web.Filters;
using Evolver.Web.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Evolver.Web.Extensions;

public static class EvolverWebServiceCollectionExtensions
{
    /// <summary>
    /// MVC + 统一响应信封过滤器。
    /// </summary>
    public static IServiceCollection AddEvolverControllersWithUnifiedApi(this IServiceCollection services)
    {
        services.AddControllers(options => { options.Filters.Add(new UnifiedApiResponseFilter()); });
        return services;
    }

    /// <summary>
    /// Swagger + JWT Bearer 安全定义（与 <see cref="AddEvolverJwtAuthentication"/> 一致）。
    /// </summary>
    public static IServiceCollection AddEvolverSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(static c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Evolver API",
                Version = "v1",
                Description = "统一响应体：`{ success, data?, code?, message?, traceId? }`（成功时 `success=true`，业务数据在 `data`）。"
            });

            const string bearerSchemeId = "Bearer";
            c.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description =
                    "先调用 POST /api/auth/login，将返回 JSON 里的 **accessToken** 整段粘贴到此处。" +
                    "不要包含单词 Bearer（Swagger 会自动加）。若仍 401，请检查是否误粘贴成 `Bearer eyJ...` 导致双重 Bearer。"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = bearerSchemeId
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// JWT Bearer 认证（读取配置节 <c>Jwt</c>）。
    /// </summary>
    public static IServiceCollection AddEvolverJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<JwtTokenService>();

        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        return services;
    }

    /// <summary>
    /// 动态策略 <c>perm:*</c> + <see cref="PermissionAuthorizationHandler"/>。
    /// </summary>
    public static IServiceCollection AddEvolverPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }

    /// <summary>
    /// Identity（<see cref="AppUser"/> / <see cref="AppRole"/>）与 EF 存储。
    /// </summary>
    public static IdentityBuilder AddEvolverIdentity(this IServiceCollection services)
    {
        return services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();
    }

    /// <summary>
    /// 多租户上下文：请求作用域，由 <see cref="TenantContextMiddleware"/> 填充。
    /// </summary>
    public static IServiceCollection AddEvolverTenantContext(this IServiceCollection services)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        return services;
    }

    /// <summary>
    /// 开发环境 CORS（全开放，仅用于本地调试）。
    /// </summary>
    public static IServiceCollection AddEvolverDevCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("dev", p =>
                p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
        });
        return services;
    }
}
