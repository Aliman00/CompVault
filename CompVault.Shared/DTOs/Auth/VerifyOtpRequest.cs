using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Auth;

/// <summary>
/// Steg 2 i passwordless-innlogging.
/// Klienten sender tilbake e-postadressen og engangs-koden de mottok.
/// </summary>
public sealed class VerifyOtpRequest
{
    /// <summary>E-postadressen til brukeren — må matche adressen fra steg 1.</summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init => field = value.Trim(); } = null!;

    /// <summary>Den 6-sifrede engangs-koden brukeren mottok.</summary>
    [Required(ErrorMessage = "OtpCode is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OtpCode must be 6-digits")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "OtpCode must consist of exactly 6 digits")]
    public string OtpCode { get; set; } = string.Empty;
}
