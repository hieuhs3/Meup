using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/medications")]
[Authorize]
public class MedicationsController : ControllerBase
{
    private readonly IMedicationService _meds;

    public MedicationsController(IMedicationService meds) => _meds = meds;

    private Guid UserId => User.GetUserId();
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [HttpGet]
    public async Task<IActionResult> GetMedications([FromQuery] DateOnly? date)
        => Ok(await _meds.GetMedicationsAsync(UserId, date ?? Today));

    [HttpPost]
    public async Task<IActionResult> CreateMedication(CreateMedicationRequest request, [FromQuery] DateOnly? date)
        => Ok(await _meds.CreateMedicationAsync(UserId, request, date ?? Today));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMedication(Guid id, UpdateMedicationRequest request, [FromQuery] DateOnly? date)
    {
        var dto = await _meds.UpdateMedicationAsync(UserId, id, request, date ?? Today);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMedication(Guid id)
        => await _meds.DeleteMedicationAsync(UserId, id) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/take")]
    public async Task<IActionResult> Take(Guid id, [FromQuery] DateOnly? date)
    {
        var dto = await _meds.SetIntakeAsync(UserId, id, date ?? Today, true);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}/take")]
    public async Task<IActionResult> Untake(Guid id, [FromQuery] DateOnly? date)
    {
        var dto = await _meds.SetIntakeAsync(UserId, id, date ?? Today, false);
        return dto is null ? NotFound() : Ok(dto);
    }
}
