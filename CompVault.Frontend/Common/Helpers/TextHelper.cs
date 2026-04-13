namespace CompVault.Frontend.Common.Helpers;

public static class TextHelper
{
    /// <summary>
    /// Trimmer en tekst til ønsket lengde. Brukes gjerne til descriptions som kan bli for lange i listevisning
    /// </summary>
    /// <param name="text">Teksten som skal trimmes</param>
    /// <param name="maxLength">Ønsket makslengde</param>
    /// <returns>Ferdig trimmet string med ...-på slutten hvis den er for lang</returns>
    public static string Truncate(string text, int maxLength) =>
        text.Length > maxLength 
            ? text[..maxLength] + "…" 
            : text;
}