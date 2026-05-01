using CompVault.Shared.Constants;

namespace CompVault.Frontend.Common.Extensions;

public static class TextExtensions
{
    /// <summary>
    /// Trimmer en tekst til ønsket lengde. Brukes gjerne til descriptions som kan bli for lange i listevisning
    /// Går til string og nullable string
    /// </summary>
    /// <param name="text">Teksten som skal trimmes</param>
    /// <param name="maxLength">Ønsket makslengde</param>
    /// <returns>Ferdig trimmet string med ...-på slutten hvis den er for lang</returns>
    public static string Truncate(this string? text, int maxLength) =>
        text is null ? string.Empty : text.Length > maxLength
            ? text[..maxLength] + "…"
            : text;

    /// <summary>
    /// Gjør om et fullName til å returnere initialene til brukeren
    /// </summary>
    /// <param name="fullName">FullName fra CircuitUserContext</param>
    /// <returns>Initialene</returns>
    public static string ToInitials(this string? fullName) =>
        string.Concat(
            (fullName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(w => w[0])
        );

    /// <summary>
    /// Oversetter data fra backend til norsk. Alternativt til localization da vi ikke har implementert det enda
    /// </summary>
    public static string ToNorwegian(this string category) => category switch
    {
        "Users" => "Brukere",
        "Roles" => "Roller",
        "Departments" => "Avdelinger",
        "Competencies" => "Kompetanser",
        "Admins" => "Administrator",
        "ApplicationUser" => "Brukere",
        "ApplicationRole" => "Roller",
        "Department" => "Avdelinger",
        "JobTitle" => "Stillingstitler",
        "Competency" => "Kompetanser",
        "CompetencyType" => "Kompetansetyper",
        "Document" => "Dokumenter",
        "DocumentType" => "Dokumenttyper",
        "DocumentTypeCategory" => "Dokumentkategorier",
        "DocumentSignature" => "Dokumentsignaturer",
        "EquipmentCategory" => "Utstyrskategorier",
        "EquipmentItem" => "Utstyr",
        "EquipmentIssuance" => "Utstyrsleveranser",
        "Permission" => "Tillatelser",
        _ => category
    };

    /// <summary>
    /// Mapper en permission til noe forståerlig for å vise permissions til en bruker.
    /// Eks: User.Read blir "Se brukere"
    /// </summary>
    /// <param name="permission">Tilattelsen vi bytter om</param>
    /// <returns>Leslig permission</returns>
    public static string ToNorwegianPermission(this string permission) => permission switch
    {
        Permissions.UsersRead => "Se brukere",
        Permissions.UsersWrite => "Opprett/endre brukere",
        Permissions.UsersDelete => "Slett brukere",
        Permissions.RolesRead => "Se roller",
        Permissions.RolesWrite => "Opprett/endre roller",
        Permissions.RolesDelete => "Slett roller",
        Permissions.DepartmentsRead => "Se avdelinger",
        Permissions.DepartmentsWrite => "Opprett/endre avdelinger",
        Permissions.DepartmentsDelete => "Slett avdelinger",
        Permissions.CompetenciesRead => "Se kompetanser",
        Permissions.CompetenciesWrite => "Opprett/endre kompetanser",
        Permissions.CompetenciesDelete => "Slett kompetanser",
        Permissions.AdminAccess => "Se administratorpanel",
        _ => permission
    };

    /// <summary>
    /// Gjør om AuditLog sin Action om til leslig og forståerlig for brukeren
    /// </summary>
    /// <param name="action">Action-feltet fra en AuditLog-entitet</param>
    /// <returns>Lesligformat på norsk</returns>
    public static string ToNorwegianAction(this string action) => action switch
    {
        // ApplicationUser
        "application_user.create" => "Bruker opprettet",
        "application_user.update" => "Bruker oppdatert",
        "application_user.delete" => "Bruker slettet",

        // ApplicationRole
        "application_role.create" => "Rolle opprettet",
        "application_role.update" => "Rolle oppdatert",
        "application_role.delete" => "Rolle slettet",

        // Department
        "department.create" => "Avdeling opprettet",
        "department.update" => "Avdeling oppdatert",
        "department.delete" => "Avdeling slettet",

        // JobTitle
        "job_title.create" => "Stillingstittel opprettet",
        "job_title.update" => "Stillingstittel oppdatert",
        "job_title.delete" => "Stillingstittel slettet",

        // Competency
        "competency.create" => "Kompetanse opprettet",
        "competency.update" => "Kompetanse oppdatert",
        "competency.delete" => "Kompetanse slettet",
        "competency.revoke" => "Kompetanse tilbakekalt",

        // CompetencyType
        "competency_type.create" => "Kompetansetype opprettet",
        "competency_type.update" => "Kompetansetype oppdatert",
        "competency_type.delete" => "Kompetansetype slettet",

        // Document
        "document.create" => "Dokument opprettet",
        "document.update" => "Dokument oppdatert",
        "document.delete" => "Dokument slettet",
        "document.signature_removed" => "Dokumentsignatur fjernet",

        // DocumentType
        "document_type.create" => "Dokumenttype opprettet",
        "document_type.update" => "Dokumenttype oppdatert",
        "document_type.delete" => "Dokumenttype slettet",

        // DocumentTypeCategory
        "document_type_category.create" => "Dokumentkategori opprettet",
        "document_type_category.update" => "Dokumentkategori oppdatert",
        "document_type_category.delete" => "Dokumentkategori slettet",

        // DocumentSignature
        "document_signature.create" => "Dokument signert",
        "document_signature.delete" => "Dokumentsignatur slettet",

        // EquipmentCategory
        "equipment_category.create" => "Utstyrskategori opprettet",
        "equipment_category.update" => "Utstyrskategori oppdatert",
        "equipment_category.delete" => "Utstyrskategori slettet",

        // EquipmentItem
        "equipment_item.create" => "Utstyr opprettet",
        "equipment_item.update" => "Utstyr oppdatert",
        "equipment_item.delete" => "Utstyr slettet",

        // EquipmentIssuance
        "equipment_issuance.create" => "Utstyr utlevert",
        "equipment_issuance.update" => "Utstyrsleveranse oppdatert",
        "equipment_issuance.delete" => "Utstyrsleveranse slettet",

        // Permission
        "permission.create" => "Tillatelse opprettet",
        "permission.update" => "Tillatelse oppdatert",
        "permission.delete" => "Tillatelse slettet",

        _ => action
    };

    /// <summary>
    /// Gjør om nøkkelen til en AuditLog sin detalje felt til norsk og mer forståerlig for brukeren
    /// </summary>
    /// <param name="key">Nøkkelen til Detail</param>
    /// <returns>Et ord/setning oversatt til norsk</returns>
    public static string ToNorwegianAuditKey(this string key) => key switch
    {
        "isactive" => "Status",
        "isleader" => "Lederrolle",
        "changed_fields" => "Endrede felter",
        "reason" => "Årsak",
        "isrevoked" => "Tilbakekalt",
        "revokedreason" => "Årsak til tilbakekalling",
        "revokedat" => "Tilbakekalt dato",
        "certificatenumber" => "Sertifikatnummer",
        "notes" => "Notater",
        "name" => "Navn",
        "description" => "Beskrivelse",
        "email" => "E-post",
        "firstname" => "Fornavn",
        "lastname" => "Etternavn",
        "employmenttype" => "Ansettelsestype",
        "category" => "Kategori",
        "username" => "Brukernavn",
        "normalizedemail" => "Normalisert e-post",
        "normalizedusername" => "Normalisert brukernavn",
        "emailconfirmed" => "E-post bekreftet",
        "phonenumberconfirmed" => "Telefon bekreftet",
        "twofactorenabled" => "To-faktor aktivert",
        "lockoutenabled" => "Utlåsing aktivert",
        "accessfailedcount" => "Mislykkede innlogginger",
        "securitystamp" => "Sikkerhetsstempel",
        "concurrencystamp" => "Samtidighetsstempel",
        _ => key
    };
}