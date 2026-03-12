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
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init => field = value.Trim(); } = null!;

    /// <summary>
    /// Hvilken kanal OTP-koden sendes på.
    /// SMS-valget vises kun i Frontend hvis brukeren har et mobilnummer registrert.
    /// </summary>
    [Required(ErrorMessage = "DeliveryMethod is required")]
    public OtpDeliveryMethod DeliveryMethod { get; set; } = OtpDeliveryMethod.Email;
}