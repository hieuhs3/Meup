using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/career")]
[Authorize]
public class CareerController : ControllerBase
{
    private readonly ICareerService _career;

    public CareerController(ICareerService career) => _career = career;

    private Guid UserId => User.GetUserId();

    // --- Skills ---

    [HttpGet("skills")]
    public async Task<IActionResult> GetSkills() => Ok(await _career.GetSkillsAsync(UserId));

    [HttpPost("skills")]
    public async Task<IActionResult> CreateSkill(SaveSkillRequest request)
        => Ok(await _career.CreateSkillAsync(UserId, request));

    [HttpPut("skills/{id:guid}")]
    public async Task<IActionResult> UpdateSkill(Guid id, SaveSkillRequest request)
    {
        var dto = await _career.UpdateSkillAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("skills/{id:guid}")]
    public async Task<IActionResult> DeleteSkill(Guid id)
        => await _career.DeleteSkillAsync(UserId, id) ? NoContent() : NotFound();

    // --- Certifications ---

    [HttpGet("certifications")]
    public async Task<IActionResult> GetCertifications() => Ok(await _career.GetCertificationsAsync(UserId));

    [HttpPost("certifications")]
    public async Task<IActionResult> CreateCertification(SaveCertificationRequest request)
        => Ok(await _career.CreateCertificationAsync(UserId, request));

    [HttpPut("certifications/{id:guid}")]
    public async Task<IActionResult> UpdateCertification(Guid id, SaveCertificationRequest request)
    {
        var dto = await _career.UpdateCertificationAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("certifications/{id:guid}")]
    public async Task<IActionResult> DeleteCertification(Guid id)
        => await _career.DeleteCertificationAsync(UserId, id) ? NoContent() : NotFound();

    // --- Projects ---

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects() => Ok(await _career.GetProjectsAsync(UserId));

    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject(SaveCareerProjectRequest request)
        => Ok(await _career.CreateProjectAsync(UserId, request));

    [HttpPut("projects/{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, SaveCareerProjectRequest request)
    {
        var dto = await _career.UpdateProjectAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("projects/{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id)
        => await _career.DeleteProjectAsync(UserId, id) ? NoContent() : NotFound();
}
