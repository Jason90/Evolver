using System.Security.Claims;
using Evolver.Infrastructure.Persistence;

namespace Evolver.Web.Security;

public sealed class TenantContextMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext, TenantContext tenantContext)
    {
        if (TryGetIntHeader(httpContext, "X-Tenant-Id", out var tenantId))
            tenantContext.TenantId = tenantId;

        if (TryGetIntHeader(httpContext, "X-Org-Id", out var orgId))
            tenantContext.OrgId = orgId;

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            if (TryGetIntClaim(user, "tenant_id", out var claimTenant))
                tenantContext.TenantId = claimTenant;

            if (TryGetIntClaim(user, "org_id", out var claimOrg))
                tenantContext.OrgId = claimOrg;

            if (long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
                tenantContext.UserId = uid;
        }

        await next(httpContext);
    }

    private static bool TryGetIntHeader(HttpContext ctx, string header, out int value)
    {
        value = default;
        if (!ctx.Request.Headers.TryGetValue(header, out var raw))
            return false;
        return int.TryParse(raw.ToString(), out value);
    }

    private static bool TryGetIntClaim(ClaimsPrincipal user, string type, out int value)
    {
        value = default;
        var raw = user.FindFirstValue(type);
        return raw is not null && int.TryParse(raw, out value);
    }
}
