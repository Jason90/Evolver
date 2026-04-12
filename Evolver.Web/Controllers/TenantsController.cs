using System.Collections.Generic;
using System.Security.Claims;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Shared.Dtos;
using Evolver.Web.Options;
using Evolver.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Evolver.Web.Controllers;

/// <summary>租户列表与管理（仅平台租户管理员）。</summary>
[ApiController]
[Route("api/tenants")]
[Authorize]
public sealed class TenantsController(
    TenantProvisioningService provisioning,
    TenantSpreadsheetService spreadsheet,
    ITenantContext tenant,
    IOptions<PlatformOptions> platformOptions,
    UserManager<AppUser> userManager
) : ControllerBase
{
    private readonly PlatformOptions _platform = platformOptions.Value;

    /// <summary>须放在无模板 <see cref="List"/> 之前，避免部分环境下子路径匹配异常。</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TenantListItemDto>> GetById(int id, CancellationToken ct)
    {
        if (!await IsPlatformAdminAsync())
            return Forbid();

        var row = await provisioning.GetByIdAsync(id, ct);
        if (row is null)
            return NotFound();
        return Ok(row);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantListItemDto>>> List(CancellationToken ct)
    {
        if (!await IsPlatformAdminAsync())
            return Forbid();

        var rows = await provisioning.ListAllAsync(ct);
        return Ok(rows);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateTenantDto dto, CancellationToken ct)
    {
        if (!await IsPlatformAdminAsync())
            return Forbid();

        try
        {
            await provisioning.UpdateTenantNameAsync(id, dto.Name, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await IsPlatformAdminAsync())
            return Forbid();

        try
        {
            await provisioning.SoftDeleteTenantAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("import")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<TenantImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (!await IsPlatformAdminAsync())
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    [HttpPost("provision")]
    public async Task<ActionResult<ProvisionTenantResponseDto>> Provision(
        [FromBody] ProvisionTenantRequestDto dto,
        CancellationToken ct)
    {
        if (!await IsPlatformAdminAsync())
            return Forbid();

        try
        {
            var res = await provisioning.ProvisionAsync(dto, ct);
            return Ok(res);
        }
        catch (InvalidOperationException ex)
        {
            // 必须传 string：UnifiedApiResponseFilter.WrapConflict 对匿名对象会回退为「与当前状态冲突。」
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<bool> IsPlatformAdminAsync()
    {
        if (tenant.TenantId != _platform.PlatformTenantId)
            return false;
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(uid) || !long.TryParse(uid, out var id))
            return false;
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, "Admin");
    }
}
