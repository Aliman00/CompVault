namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for CompetencyType-featuren.
/// Ved oppdatering så må både variablene og Error-feltene endres
/// </summary>
public class CompTypeValidations
{
    public const int NameMaxLength = 200;
    public const int DescMaxLength = 500;
    public const int CategoryMaxLength = 100;

    public static class Errors
    {
        public const string NameRequired = "Navn på kompetansetypen er påkrevd";
        public const string NameMaxLength = "Navn kan ikke være lengre enn 200 tegn";

        public const string DescMaxLength = "Beskrivelse kan ikke være lengre enn 500 tegn";

        public const string CategoryMaxLength = "Kategori kan ikke være lengre enn 100 tegn";
    }
}