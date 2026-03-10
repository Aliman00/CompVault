using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Users;

/// <summary>
/// Felt som kan oppdateres på en bruker. Bare felter som er satt (ikke null) blir endret.
/// </summary>
public sealed class UpdateUserRequest
{
    /// <summary>Nytt fornavn (valgfritt).</summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>Nytt etternavn (valgfritt).</summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>Ny stillingstittel (valgfritt).</summary>
    [MaxLength(150)]
    public string? JobTitle { get; set; }

    /// <summary>Ny ansettelsestype (valgfritt).</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>Aktiver eller deaktiver kontoen (valgfritt).</summary>
    public bool? IsActive { get; set; }

    /// <summary>Flytt brukeren til en annen avdeling (valgfritt).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Bytt leder (valgfritt).</summary>
    public Guid? ManagerId { get; set; }
}
