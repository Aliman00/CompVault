namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for JobTitle-featuren.
/// Ved oppdatering så må både variabelene og Error-feltene endres
/// </summary>
public static class JobTitleValidations
{
    public const int NameMaxLength = 100;

    public static class Errors
    {
        public const string NameRequired = "Stillingstittel er påkrevd";
        public const string NameMaxLength = "Stillingstittel kan ikke være lengre enn 100 tegn";
    }
}