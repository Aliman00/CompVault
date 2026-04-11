namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for User-featuren. Ved oppdatering så må både variabelene og Error-feltene endres   
/// </summary>
public static class UserValidations
{
    public const int FirstNameMaxLength = 100;
    public const int LastNameMaxLength = 100;
    public const int EmailMaxLength = 256;
    public const int JobTitleMaxLength = 150;
    
    public static class Errors
    {
        public const string FirstNameRequired = "Fornavn er påkrevd";
        public const string FirstNameMaxLength = "Fornavn kan ikke være lengre enn 100 tegn";

        public const string LastNameRequired = "Etternavn er påkrevd";
        public const string LastNameMaxLength = "Etternavn kan ikke være lengre enn 100 tegn";

        public const string EmailRequired = "E-post er påkrevd";
        public const string EmailInvalid = "Ugyldig e-postadresse";
        public const string EmailMaxLength = "E-post kan ikke være mer enn 256 tegn";

        public const string JobTitleMaxLength = "Stillingstittel kan ikke være lengre enn 150 tegn";
    }
}