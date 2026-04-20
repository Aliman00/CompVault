using System.Text;
using System.Text.RegularExpressions;

namespace CompVault.Backend.Common.Utils;

/// <summary>
/// Genererer URL-vennlige og filsystem-trygge slug-verdier fra visningsnavn.
/// Eksempel: "Stillings Instruks" → "stillings-instruks".
/// </summary>
public static partial class SlugUtility
{
    private const int MaxSlugLength = 50;
    private static readonly Regex ValidSlugPattern = SlugRegex();

    /// <summary>
    /// Konverterer et visningsnavn til en URL-vennlig slug.
    /// </summary>
    /// <param name="name">Visningsnavn, f.eks. "Stillings Instruks".</param>
    /// <returns>Slug, f.eks. "stillings-instruks".</returns>
    /// <exception cref="ArgumentException">Kastes hvis <paramref name="name"/> er null/tom eller resultatet blir tomt.</exception>
    public static string GenerateSlug(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var slug = new StringBuilder(name.Trim().ToLowerInvariant());

        // Norske tegn → ASCII-ekvivalenter
        slug.Replace("æ", "ae");
        slug.Replace("ø", "oe");
        slug.Replace("å", "aa");

        // Mellomrom og whitespace → bindestrek
        for (int i = 0; i < slug.Length; i++)
        {
            if (char.IsWhiteSpace(slug[i]))
                slug[i] = '-';
        }

        // Behold kun a-z, 0-9 og bindestrek
        var cleaned = new StringBuilder(slug.Length);
        foreach (char c in slug.ToString())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
                cleaned.Append(c);
        }

        // Kollaps multiple bindestreker, trim fra start/slutt
        string result = Regex.Replace(cleaned.ToString(), "-{2,}", "-").Trim('-');

        // Truncate til max 50 tegn
        if (result.Length > MaxSlugLength)
            result = result[..MaxSlugLength].TrimEnd('-');

        if (string.IsNullOrWhiteSpace(result))
            throw new ArgumentException(
                $"Kunne ikke generere slug fra navn '{name}'. Navnet inneholder ingen gyldige tegn.", nameof(name));

        return result;
    }

    /// <summary>
    /// Sjekker om en slug har gyldig format: kun små bokstaver, tall og bindestreker.
    /// </summary>
    internal static bool IsValidSlug(string slug)
        => !string.IsNullOrWhiteSpace(slug) && ValidSlugPattern.IsMatch(slug);

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugRegex();
}