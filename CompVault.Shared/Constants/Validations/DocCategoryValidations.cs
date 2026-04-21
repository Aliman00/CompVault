namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for DocumentTypeCategory-featuren.
/// Ved oppdatering så må både variablene og Error-feltene endres
/// </summary>
public static class DocCategoryValidations
{
    public const int NameMaxLength = 100;

    public static class Errors
    {
        public const string NameRequired = "Navn på kategorien er påkrevd";
        public const string NameMaxLength = "Navn kan ikke være lengre enn 100 tegn";
    }
}