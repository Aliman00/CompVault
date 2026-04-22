namespace CompVault.Frontend.Common.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Gir oss antall timer, dager eller år siden utifra en DateTime
    /// </summary>
    /// <param name="utcDate">DateTime-objekt med UTC</param>
    /// <returns>En ferdig formatert string</returns>
    public static string ToRelativeNorwegian(this DateTime utcDate)
    {
        TimeSpan diff = DateTime.UtcNow - utcDate;

        return diff.TotalSeconds switch
        {
            < 60 => "akkurat nå",
            < 3600 => $"for {(int)diff.TotalMinutes} minutt{((int)diff.TotalMinutes == 1 ? "" : "er")} siden",
            < 86400 => $"for {(int)diff.TotalHours} time{((int)diff.TotalHours == 1 ? "" : "r")} siden",
            < 7 * 86400 => $"for {(int)diff.TotalDays} dag{((int)diff.TotalDays == 1 ? "" : "er")} siden",
            < 30 * 86400 => $"for {(int)(diff.TotalDays / 7)} uke{((int)(diff.TotalDays / 7) == 1 ? "" : "r")} siden",
            < 365 * 86400 => $"for {(int)(diff.TotalDays / 30)} måned{((int)(diff.TotalDays / 30) == 1 ? "" : "er")} siden",
            _ => $"for {(int)(diff.TotalDays / 365)} år siden"
        };
    }

}