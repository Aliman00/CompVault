namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for DocumentType-featuren.
/// Ved oppdatering så må både variablene og Error-feltene endres
/// </summary>
public static class DocTypeValidations
{
    public const int NameMaxLength = 100;
    public const int SlugMaxLength = 50;
    public const int DescMaxLength = 500;

    public static class Errors
    {
        public const string NameRequired = "Navn på dokumenttypen er påkrevd";
        public const string NameMaxLength = "Navn kan ikke være lengre enn 100 tegn";

        public const string SlugRequired = "Slug er påkrevd";
        public const string SlugMaxLength = "Slug kan ikke være lengre enn 50 tegn";

        public const string DescMaxLength = "Beskrivelse kan ikke være lengre enn 500 tegn";

        public const string TargetModeRequired = "Målgruppemodus er påkrevd";
    }
}