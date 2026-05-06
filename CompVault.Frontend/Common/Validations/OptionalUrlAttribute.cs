using System.ComponentModel.DataAnnotations;
namespace CompVault.Frontend.Common.Validations;

public class OptionalUrlAttribute : ValidationAttribute
{
    // Overstyerer URL slik at hvis vi kan hoppe ut av et URL-felt selvom verdien er en tom string
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
            return ValidationResult.Success;

        string url = (string)value;

        bool isValid = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                       && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        return isValid
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage, [validationContext.MemberName!]);
    }
}