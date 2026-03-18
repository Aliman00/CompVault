using MudBlazor;

namespace CompVault.Frontend.Common.Themes;

public static class CompVaultMainTheme
{
    public static MudTheme Theme => new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2d7a4f",
            PrimaryDarken = "#1f5c3a",
            PrimaryLighten = "#4ecf7a",
            Secondary = "#1a2535",
            Tertiary = "#c9a84c",
            AppbarBackground = "#1a2535",
            AppbarText = "#ffffff",
            Background = "#ffffff",
            BackgroundGray = "#f4f6f8",
            DrawerBackground = "#1a2535",
            DrawerText = "#e0e0e0",
            DrawerIcon = "#4ecf7a",
            Success = "#4ecf7a",
            TextPrimary = "#1a2535",
            TextSecondary = "#4a5568",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#4ecf7a",
            PrimaryDarken = "#2d7a4f",
            Secondary = "#c9a84c",
            AppbarBackground = "#0f1923",
            Background = "#121212",
            BackgroundGray = "#1e1e1e",
            DrawerBackground = "#0f1923",
            DrawerText = "#e0e0e0",
            DrawerIcon = "#4ecf7a",
            Success = "#4ecf7a",
            TextPrimary = "#e0e0e0",
            TextSecondary = "#9e9e9e",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Roboto", "sans-serif"]
            }
        }
    };
}