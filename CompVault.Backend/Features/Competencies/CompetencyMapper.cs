using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.DTOs.CompetencyTypes;

namespace CompVault.Backend.Features.Competencies;

/// <summary>
/// Mapper for konvertering mellom Competency/CompetencyType entities og DTOs.
/// </summary>
public static class CompetencyMapper
{
    /// <summary>
    /// Konverterer en <see cref="CompetencyType"/> til en <see cref="CompetencyTypeDto"/>.
    /// </summary>
    public static CompetencyTypeDto ToTypeDto(CompetencyType type) => new()
    {
        Id = type.Id,
        Name = type.Name,
        Description = type.Description,
        Category = type.Category,
        RequiresExpiration = type.RequiresExpiration,
        CreatedAt = type.CreatedAt,
        IsActive = type.IsActive
    };

    /// <summary>
    /// Konverterer en <see cref="Competency"/> til en <see cref="CompetencyDto"/>.
    /// Beregner <see cref="CompetencyDto.DaysUntilExpiry"/> basert på utløpsdato.
    /// </summary>
    public static CompetencyDto ToDto(Competency competency)
    {
        DateTime now = DateTime.UtcNow;

        return new CompetencyDto
        {
            Id = competency.Id,
            UserId = competency.UserId,
            UserName = competency.ApplicationUser?.UserName,
            UserFirstName = competency.ApplicationUser?.FirstName,
            UserLastName = competency.ApplicationUser?.LastName,
            CompetencyTypeId = competency.CompetencyTypeId,
            CompetencyTypeName = competency.CompetencyType?.Name,
            CompetencyTypeRequiresExpiration = competency.CompetencyType?.RequiresExpiration ?? true,
            Status = competency.Status,
            ExpiryDate = competency.ExpiryDate,
            IssuedDate = competency.IssuedDate,
            CertificateNumber = competency.CertificateNumber,
            Notes = competency.Notes,
            DaysUntilExpiry = CalculateDaysUntilExpiry(competency.ExpiryDate, now),
            CreatedAt = competency.CreatedAt,
            RevokedAt = competency.RevokedAt,
            RevokedReason = competency.RevokedReason
        };
    }

    /// <summary>
    /// Konverterer en <see cref="Competency"/> til en <see cref="ExpiringCompetencyDto"/>.
    /// Inkluderer avdelingsinfo og beregnet dager til utløp.
    /// </summary>
    public static ExpiringCompetencyDto ToExpiringDto(Competency competency)
    {
        DateTime now = DateTime.UtcNow;

        return new ExpiringCompetencyDto
        {
            Id = competency.Id,
            CompetencyTypeName = competency.CompetencyType?.Name,
            Status = competency.Status,
            UserId = competency.UserId,
            UserName = competency.ApplicationUser?.UserName,
            UserFirstName = competency.ApplicationUser?.FirstName,
            UserLastName = competency.ApplicationUser?.LastName,
            ExpiryDate = competency.ExpiryDate,
            DaysUntilExpiry = CalculateDaysUntilExpiry(competency.ExpiryDate, now),
            DepartmentId = competency.ApplicationUser?.DepartmentId,
            DepartmentName = competency.ApplicationUser?.Department?.Name
        };
    }

    private static int? CalculateDaysUntilExpiry(DateTime? expiryDate, DateTime now) =>
        expiryDate.HasValue ? (int)Math.Floor((expiryDate.Value - now).TotalDays) : null;
}