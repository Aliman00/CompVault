using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Auth;

/// <summary>
/// Steg 1 i passwordless-innlogging.
/// Klienten sender e-postadressen og velger hvilken kanal engangs-koden skal leveres på.
/// </summary>
public sealed class RequestOtpRequest
{
    /// <summary>E-postadressen til brukeren.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hvilken kanal OTP-koden sendes på.
    /// SMS-valget vises kun i Frontend hvis brukeren har et mobilnummer registrert.
    /// </summary>
    [Required]
    public OtpDeliveryMethod DeliveryMethod { get; set; } = OtpDeliveryMethod.Email;
}
