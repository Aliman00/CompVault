namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles valideringsregler for Role-featuren.
/// Ved oppdatering må både variablene og Error-feltene endres.
/// </summary>
public static class RoleValidations
{
    // Roles
    public const int NameMinLength = 2;
    public const int NameMaxLength = 256;
    public const int DescriptionMinLength = 5;
    public const int DescriptionMaxLength = 250;

    // Permissions
    public const int PermissionNamesMaxCount = 50;

    public static class Errors
    {
        // Roles
        public const string NameRequired = "Rollenavn er påkrevd";
        public const string NameMinLength = "Rollenavn må være minst 2 tegn";
        public const string NameMaxLength = "Rollenavn kan ikke være lengre enn 256 tegn";

        public const string DescriptionRequired = "Beskrivelse er påkrevd";
        public const string DescriptionMinLength = "Beskrivelse må være minst 5 tegn";
        public const string DescriptionMaxLength = "Beskrivelse kan ikke være lengre enn 250 tegn";

        // Permissions
        public const string PermissionNamesRequired = "Permissions er påkrevd";
        public const string PermissionNamesMaxCount = "Maks 50 permissions per forespørsel";
    }
}