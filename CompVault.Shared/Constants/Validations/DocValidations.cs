namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for Document-featuren.
/// Ved oppdatering så må både variablene og Error-feltene endres
/// </summary>
public static class DocValidations
{
    public const int TitleMaxLength = 200;
    public const int DescMaxLength = 2000;
    public const int ExternalUrlMaxLength = 500;

    public static class Errors
    {
        public const string TitleRequired = "Dokumenttittel er påkrevd";
        public const string TitleMaxLength = "Tittel kan ikke være lengre enn 200 tegn";

        public const string DescMaxLength = "Beskrivelse kan ikke være lengre enn 2000 tegn";

        public const string ExternalUrlMaxLength = "URL kan ikke være lengre enn 500 tegn";
        public const string ExternalUrlFormat = "Ugyldig URL-format";
    }
}