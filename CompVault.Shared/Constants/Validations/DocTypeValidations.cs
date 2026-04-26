namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for DocumentType-featuren.
/// Ved oppdatering så må både variablene og Error-feltene endres
/// </summary>
public static class DocTypeValidations
{
    public const int NameMaxLength = 100;
    public const int DescMaxLength = 500;
    
    // 1 byte til 100 MB
    public const int MaxFileSizeMinBytes = 1;
    public const long MaxFileSizeMaxBytes = 100L * 1024 * 1024;

    // NB: Disse to konstantene er avledet fra MaxFileSizeMaxBytes.
    // Ved endring der oppe MÅ både den og MaxFileSizeRange-feilmeldingen oppdateres.
    public const int MaxFileSizeMinMb = 1;
    public const int MaxFileSizeMaxMb = 100;

    public static class Errors
    {
        public const string NameRequired = "Navn på dokumentkategorien er påkrevd";
        public const string NameMaxLength = "Navn kan ikke være lengre enn 100 tegn";

        public const string DescMaxLength = "Beskrivelse kan ikke være lengre enn 500 tegn";

        public const string TargetModeRequired = "Målgruppemodus er påkrevd";
        public const string MaxFileSizeRange = "Maksimal filstørrelse må være mellom 1 byte og 100 MB.";
    }
}