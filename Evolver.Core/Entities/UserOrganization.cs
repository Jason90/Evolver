namespace Evolver.Core.Entities;

/// <summary>
/// Many-to-many: user membership in organizations (teams/branches).
/// </summary>
public sealed class UserOrganization : BaseEntity
{
    public long UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public long OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}
