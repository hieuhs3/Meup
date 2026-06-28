using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class CareerService : ICareerService
{
    private readonly AppDbContext _db;

    public CareerService(AppDbContext db) => _db = db;

    // --- Skill ---

    private static SkillDto ToDto(Skill s) => new(s.Id, s.Name, s.Category, s.Level, s.CreatedAt);

    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(Guid userId) =>
        await _db.Skills.Where(s => s.UserId == userId)
            .OrderBy(s => s.Category).ThenByDescending(s => s.Level).ThenBy(s => s.Name)
            .Select(s => ToDto(s)).ToListAsync();

    public async Task<SkillDto> CreateSkillAsync(Guid userId, SaveSkillRequest request)
    {
        var s = new Skill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Level = Math.Clamp(request.Level, 1, 5),
        };
        _db.Skills.Add(s);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<SkillDto?> UpdateSkillAsync(Guid userId, Guid id, SaveSkillRequest request)
    {
        var s = await _db.Skills.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (s is null) return null;
        s.Name = request.Name.Trim();
        s.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        s.Level = Math.Clamp(request.Level, 1, 5);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<bool> DeleteSkillAsync(Guid userId, Guid id)
    {
        var s = await _db.Skills.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (s is null) return false;
        _db.Skills.Remove(s);
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Certification ---

    private static CertificationDto ToDto(Certification c) =>
        new(c.Id, c.Name, c.Issuer, c.IssuedAt, c.ExpiresAt, c.CreatedAt);

    public async Task<IReadOnlyList<CertificationDto>> GetCertificationsAsync(Guid userId) =>
        await _db.Certifications.Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt).ThenBy(c => c.Name)
            .Select(c => ToDto(c)).ToListAsync();

    public async Task<CertificationDto> CreateCertificationAsync(Guid userId, SaveCertificationRequest request)
    {
        var c = new Certification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Issuer = string.IsNullOrWhiteSpace(request.Issuer) ? null : request.Issuer.Trim(),
            IssuedAt = request.IssuedAt,
            ExpiresAt = request.ExpiresAt,
        };
        _db.Certifications.Add(c);
        await _db.SaveChangesAsync();
        return ToDto(c);
    }

    public async Task<CertificationDto?> UpdateCertificationAsync(Guid userId, Guid id, SaveCertificationRequest request)
    {
        var c = await _db.Certifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (c is null) return null;
        c.Name = request.Name.Trim();
        c.Issuer = string.IsNullOrWhiteSpace(request.Issuer) ? null : request.Issuer.Trim();
        c.IssuedAt = request.IssuedAt;
        c.ExpiresAt = request.ExpiresAt;
        await _db.SaveChangesAsync();
        return ToDto(c);
    }

    public async Task<bool> DeleteCertificationAsync(Guid userId, Guid id)
    {
        var c = await _db.Certifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (c is null) return false;
        _db.Certifications.Remove(c);
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Project ---

    private static CareerProjectDto ToDto(CareerProject p) =>
        new(p.Id, p.Name, p.Role, p.Description, p.StartedAt, p.EndedAt, p.CreatedAt);

    public async Task<IReadOnlyList<CareerProjectDto>> GetProjectsAsync(Guid userId) =>
        await _db.CareerProjects.Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartedAt).ThenBy(p => p.Name)
            .Select(p => ToDto(p)).ToListAsync();

    public async Task<CareerProjectDto> CreateProjectAsync(Guid userId, SaveCareerProjectRequest request)
    {
        var p = new CareerProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            StartedAt = request.StartedAt,
            EndedAt = request.EndedAt,
        };
        _db.CareerProjects.Add(p);
        await _db.SaveChangesAsync();
        return ToDto(p);
    }

    public async Task<CareerProjectDto?> UpdateProjectAsync(Guid userId, Guid id, SaveCareerProjectRequest request)
    {
        var p = await _db.CareerProjects.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (p is null) return null;
        p.Name = request.Name.Trim();
        p.Role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role.Trim();
        p.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        p.StartedAt = request.StartedAt;
        p.EndedAt = request.EndedAt;
        await _db.SaveChangesAsync();
        return ToDto(p);
    }

    public async Task<bool> DeleteProjectAsync(Guid userId, Guid id)
    {
        var p = await _db.CareerProjects.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (p is null) return false;
        _db.CareerProjects.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }
}
