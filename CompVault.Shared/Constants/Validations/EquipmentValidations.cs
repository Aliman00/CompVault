namespace CompVault.Shared.Constants.Validations;

/// <summary>
/// Valideringskonstanter for utstyrsmodulen.
/// </summary>
public static class EquipmentValidations
{
    // Lengdebegrensninger
    public const int CategoryNameMaxLength = 100;
    public const int DescriptionMaxLength = 300;
    public const int ItemNameMaxLength = 200;
    public const int SizeMaxLength = 20;
    public const int NotesMaxLength = 500;
    public const int QuantityMin = 1;
    public const int QuantityMax = 100;

    public static class Errors
    {
        // Category
        public const string NameRequired = "Navn er påkrevd";
        public const string CategoryNameMaxLength = "Navn kan ikke være lengre enn 100 tegn";
        public const string DescriptionMaxLength = "Beskrivelse kan ikke være lengre enn 300 tegn";

        // Item
        public const string ItemNameRequired = "Navn på utstyr er påkrevd";
        public const string ItemNameMaxLength = "Navn på utstyr kan ikke være lengre enn 200 tegn";

        // Issuance
        public const string QuantityRange = "Antall må være mellom 1 og 100";
        public const string SizeRequired = "Størrelse er påkrevd for dette utstyret";
        public const string SizeMaxLength = "Størrelse kan ikke være lengre enn 20 tegn";
        public const string NotesMaxLength = "Notater kan ikke være lengre enn 500 tegn";
    }
}