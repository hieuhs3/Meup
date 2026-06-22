using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _export;

    public ExportController(IExportService export) => _export = export;

    [HttpGet]
    public async Task<IActionResult> Export()
        => Ok(await _export.ExportAsync(User.GetUserId()));
}
