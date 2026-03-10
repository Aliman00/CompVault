using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Auth;

/// <summary>
/// Steg 2 i passwordless-innlogging.
/// Klienten sender tilbake e-postadressen og engangs-koden de mottok.
/// </summary>
public sealed class VerifyOtpRequest
{
    /// <summary>E-postadressen til brukeren — må matche adressen fra steg 1.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Den 6-sifrede engangs-koden brukeren mottok.</summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}
