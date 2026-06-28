using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _docs;

    public DocumentsController(IDocumentService docs) => _docs = docs;

    private Guid UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] string? category)
        => Ok(await _docs.GetDocumentsAsync(UserId, category));

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)] // 10MB + overhead
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, [FromForm] string? category)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "Chưa chọn file." });

        await using var stream = file.OpenReadStream();
        var result = await _docs.UploadAsync(UserId, category, file.FileName, file.ContentType, file.Length, stream);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var result = await _docs.DownloadAsync(UserId, id);
        if (result is null) return NotFound();
        var (meta, content) = result.Value;
        return File(content, meta.ContentType, meta.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await _docs.DeleteAsync(UserId, id) ? NoContent() : NotFound();
}
