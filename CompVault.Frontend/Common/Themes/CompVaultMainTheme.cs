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
            Primary = "#5dd887",
            PrimaryDarken = "#3da861",
            PrimaryLighten = "#7ee5a0",
            Secondary = "#d4b85a",
            Tertiary = "#6b9fff",
            AppbarBackground = "#1a2738",
            AppbarText = "#f0f0f0",
            Background = "#1e2937",
            BackgroundGray = "#2a3a4d",
            Surface = "#263545",
            DrawerBackground = "#1a2738",
            DrawerText = "#f0f0f0",
            DrawerIcon = "#5dd887",
            Success = "#5dd887",
            TextPrimary = "#f5f5f5",
            TextSecondary = "#b0c0d0",
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