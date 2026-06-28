using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface ICareerService
{
    // Skill
    Task<IReadOnlyList<SkillDto>> GetSkillsAsync(Guid userId);
    Task<SkillDto> CreateSkillAsync(Guid userId, SaveSkillRequest request);
    Task<SkillDto?> UpdateSkillAsync(Guid userId, Guid id, SaveSkillRequest request);
    Task<bool> DeleteSkillAsync(Guid userId, Guid id);

    // Certification
    Task<IReadOnlyList<CertificationDto>> GetCertificationsAsync(Guid userId);
    Task<CertificationDto> CreateCertificationAsync(Guid userId, SaveCertificationRequest request);
    Task<CertificationDto?> UpdateCertificationAsync(Guid userId, Guid id, SaveCertificationRequest request);
    Task<bool> DeleteCertificationAsync(Guid userId, Guid id);

    // Project
    Task<IReadOnlyList<CareerProjectDto>> GetProjectsAsync(Guid userId);
    Task<CareerProjectDto> CreateProjectAsync(Guid userId, SaveCareerProjectRequest request);
    Task<CareerProjectDto?> UpdateProjectAsync(Guid userId, Guid id, SaveCareerProjectRequest request);
    Task<bool> DeleteProjectAsync(Guid userId, Guid id);
}
