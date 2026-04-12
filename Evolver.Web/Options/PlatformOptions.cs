namespace Evolver.Web.Options;

/// <summary>
/// 平台租户（用于开通新租户、复制权限模板等）。默认与种子数据租户 Id=1 一致。
/// </summary>
public sealed class PlatformOptions
{
    public const string SectionName = "Platform";

    /// <summary>具备平台级租户管理（含开通新租户）能力的源租户 Id（通常为种子租户 1）。</summary>
    public int PlatformTenantId { get; set; } = 1;

    /// <summary>复制权限模板时使用的组织 Id（与种子中根组织一致，一般为 1）。</summary>
    public int TemplateOrgId { get; set; } = 1;
}
