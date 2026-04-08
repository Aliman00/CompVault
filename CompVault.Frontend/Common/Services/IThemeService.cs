using MudBlazor;

namespace CompVault.Frontend.Common.Services;

public interface IThemeService
{
    /// <summary>
    /// Henter valgt lyst/mørkt modus ved å først sjekke localstorage eller fallback til nettleseren sin modus
    /// </summary>
    /// <param name="provider">MudThemeProvider som styrer fargene til hver modus</param>
    /// <returns>True hvis dark mode, false hvis light mode</returns>
    Task<bool> GetInitialDarkModeAsync(MudThemeProvider provider);

    /// <summary>
    /// Lagrer valget av lys/mørkt modus i localstorage
    /// </summary>
    /// <param name="isDarkMode"></param>
    /// <returns></returns>
    Task SaveDarkModeAsync(bool isDarkMode);
}