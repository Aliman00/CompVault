using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Shared.DTOs.JobTitles;

namespace CompVault.Backend.Features.JobTitles;

/// <summary>
/// Mapper for konvertering mellom <see cref="JobTitle"/> og <see cref="JobTitleDto"/>.
/// </summary>
public static class JobTitleMapper
{
    /// <summary>
    /// Konverterer en <see cref="JobTitle"/> til en <see cref="JobTitleDto"/>.
    /// </summary>
    public static JobTitleDto ToDto(JobTitle jobTitle) => new()
    {
        Id = jobTitle.Id,
        Name = jobTitle.Name
    };
}
