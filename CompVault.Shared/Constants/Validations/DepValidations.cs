namespace CompVault.Shared.Constants.Validations;


/// <summary>
/// Felles klasse for valideringer for Department-featuren.
/// Ved oppdatering så må både variablene og Error-feltene endres
/// </summary>
public static class DepValidations
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 500;

    public static class Errors
    {
        public const string NameRequired = "Avdelingsnavn er påkrevd";
        public const string NameMaxLength = "Avdelingsnavn kan ikke være lengre enn 200 tegn";

        public const string DescriptionMaxLength = "Beskrivelse kan ikke være lengre enn 500 tegn";
    }
}