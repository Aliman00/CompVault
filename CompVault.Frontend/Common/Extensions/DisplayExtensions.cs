namespace CompVault.Frontend.Common.Extensions;

public static class DisplayExtensions
{
    public static string DashIfEmpty(this string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    
}