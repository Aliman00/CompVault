using CompVault.Shared.Constants;

namespace CompVault.Frontend.Common.Extensions;

public static class TextHelper
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
    /// Oversetter data fra backend til norsk. Alternativt til localization da vi ikke har implementert det enda. 
    /// </summary>
    public static string ToNorwegian(this string category) => category switch
    {
        "Users" => "Brukere",
        "Roles" => "Roller",
        "Departments" => "Avdelinger",
        "Competencies" => "Kompetanser",
        "Admins" => "Administrator",
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
}