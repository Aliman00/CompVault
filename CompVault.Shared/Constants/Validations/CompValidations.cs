namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Felles klasse for valideringer for Competency-featuren.
/// Ved oppdatering så må både variablene og Error-feltene endres.
/// </summary>
public static class CompValidations
{
    public const int CertificateNumberMaxLength = 100;
    public const int NotesMaxLength = 2000;
    public const int RevokedReasonMaxLength = 500;

    public static class Errors
    {
        public const string UserIdRequired = "Bruker-ID er påkrevd";
        public const string CompetencyTypeIdRequired = "Kompetansetype-ID er påkrevd";
        public const string IssuedDateRequired = "Utstedelsesdato er påkrevd";

        public const string CertNumberMaxLength = "Sertifikatnummer kan ikke være lengre enn 100 tegn";
        public const string NotesMaxLength = "Notater kan ikke være lengre enn 2000 tegn";

        public const string RevokedReasonMaxLength = "Årsak til tilbakekalling kan ikke være lengre enn 500 tegn";
    }
}