using CompVault.Frontend.Common.Constants;
using CompVault.Frontend.Common.Extensions;

using Microsoft.JSInterop;

using MudBlazor;

namespace CompVault.Frontend.Common.Services;

public class ThemeService(IJSRuntime jsRuntime) : IThemeService
{
    /// <inheritdoc />
    public async Task<bool> GetInitialDarkModeAsync(MudThemeProvider provider)
    {
        string? savedThemeMode = await jsRuntime.GetItemAsync(AppConstants.DarkModeKey);

        if (savedThemeMode != null && bool.TryParse(savedThemeMode, out bool savedMode))
            return savedMode;

        return await provider.GetSystemDarkModeAsync();
    }

    /// <inheritdoc />
    public async Task SaveDarkModeAsync(bool isDarkMode) =>
        await jsRuntime.SetItemAsync(AppConstants.DarkModeKey, isDarkMode.ToString());
}