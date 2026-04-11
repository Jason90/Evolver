using Evolver.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Security;

public sealed class PermissionAuthorizationHandler(AppDbContext db) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var roleNames = context.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        if (roleNames.Count == 0)
            return;

        var has = await db.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .AnyAsync(rp => roleNames.Contains(rp.Role.Name!) && rp.Permission.Code == requirement.Code);

        if (has)
            context.Succeed(requirement);
    }
}
