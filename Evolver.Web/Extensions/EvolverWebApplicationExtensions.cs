using Evolver.Web.Middleware;
using Evolver.Web.Security;
using Serilog;

namespace Evolver.Web.Extensions;

public static class EvolverWebApplicationExtensions
{
    /// <summary>
    /// 典型顺序：全局异常 → Serilog → Swagger/CORS（开发）→ HTTPS → 认证 → 多租户上下文（依赖已认证用户的 Claims）→ 授权 → 端点。
    /// </summary>
    public static WebApplication UseEvolverApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("dev");
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseMiddleware<TenantContextMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
