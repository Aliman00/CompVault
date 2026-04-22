using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Users;

/// <summary>
/// Det klienten ser når de spør etter en bruker. Ingen sensitiv info her.
/// </summary>
public sealed class UserDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>E-postadresse.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Fornavn.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Etternavn.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Fullt navn — satt sammen automatisk.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>ID til brukerens stillingstittel.</summary>
    public Guid? JobTitleId { get; set; }

    /// <summary>Navn på stillingstittelen.</summary>
    public string? JobTitleName { get; set; }

    /// <summary>Er stillingstittelen aktiv (standard som true hvis satt).</summary>
    public bool? JobTitleIsActive { get; set; } = true;

    /// <summary>Ansettelsestype.</summary>
    public EmploymentType EmploymentType { get; set; }

    /// <summary>Om kontoen er aktiv.</summary>
    public bool IsActive { get; set; }

    /// <summary>Avdelings-ID (hvis satt).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Avdelingsnavn (hvis satt).</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Er avdeling aktiv (standard som true hvis satt).</summary>
    public bool? DepartmentIsActive { get; set; } = true;

    /// <summary>Leder-ID (hvis satt).</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Ledernavn (hvis satt).</summary>
    public string? ManagerName { get; set; } = string.Empty;

    /// <summary>Når brukeren ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Rollene brukeren har.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];
}