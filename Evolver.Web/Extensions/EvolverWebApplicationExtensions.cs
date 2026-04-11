using Evolver.Web.Middleware;
using Evolver.Web.Security;
using Serilog;

namespace Evolver.Web.Extensions;

public static class EvolverWebApplicationExtensions
{
    /// <summary>
    /// 典型顺序：全局异常 → Serilog 请求日志 → Swagger/CORS（开发）→ HTTPS → 多租户 → 认证 → 授权 → 端点。
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
        app.UseMiddleware<TenantContextMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
