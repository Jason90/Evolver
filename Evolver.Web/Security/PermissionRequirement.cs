using Microsoft.AspNetCore.Authorization;

namespace Evolver.Web.Security;

public sealed class PermissionRequirement(string code) : IAuthorizationRequirement
{
    public string Code { get; } = code;
}
