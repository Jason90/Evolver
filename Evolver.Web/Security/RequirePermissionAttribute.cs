using Microsoft.AspNetCore.Authorization;

namespace Evolver.Web.Security;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string code) => Policy = $"perm:{code}";
}
