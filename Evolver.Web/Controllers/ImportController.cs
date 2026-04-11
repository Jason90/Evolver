using Evolver.Application.Import;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/import")]
[Authorize]
public sealed class ImportController(ExcelImportService importer) : ControllerBase
{
    [HttpPost("xlsx")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportResultDto>> ImportXlsx(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Empty file");

        await using var stream = file.OpenReadStream();
        var result = await importer.ImportAsync(stream);
        return Ok(result);
    }
}
