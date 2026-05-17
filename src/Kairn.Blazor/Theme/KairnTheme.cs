using MudBlazor;

namespace Kairn.Blazor.Theme;

public static class KairnTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            // Lichen – primary brand
            Primary         = "#3A9463",
            PrimaryDarken   = "#1F6040",
            PrimaryLighten  = "#E8F4ED",

            // Slate – secondary accent
            Secondary       = "#4D7A9E",
            SecondaryDarken = "#2A4F6E",
            SecondaryLighten= "#EAF0F6",

            // Semantic
            Success         = "#3A9463",   // Lichen 500
            Warning         = "#D4920F",   // Summit 500
            Error           = "#C2492A",   // Signal 500
            Info            = "#4D7A9E",   // Slate 500

            // Surfaces
            Background      = "#F5F4F1",   // Stone 50
            Surface         = "#FFFFFF",
            DrawerBackground= "#1F6040",   // Lichen 700
            DrawerText      = "#E8F4ED",   // Lichen 50
            DrawerIcon      = "#A8D9BC",   // Lichen 200

            // Text
            TextPrimary     = "#1F1E1C",   // Stone 900
            TextSecondary   = "#8C8980",   // Stone 500
            TextDisabled    = "#D6D3CA",   // Stone 200

            // Lines
            Divider         = "#D6D3CA",   // Stone 200
            TableLines      = "#D6D3CA",
            TableStriped    = "#E8F4ED",   // Lichen 50

            AppbarBackground = "#1F6040",  // Lichen 700
            AppbarText       = "#E8F4ED",  // Lichen 50
        },
        PaletteDark = new PaletteDark
        {
            Primary         = "#3A9463",   // Lichen 500
            PrimaryDarken   = "#A8D9BC",   // Lichen 200
            PrimaryLighten  = "#0D3322",   // Lichen 900
            Background      = "#1F1E1C",   // Stone 900
            Surface         = "#2C2B29",
            TextPrimary     = "#F5F4F1",   // Stone 50
            TextSecondary   = "#8C8980",   // Stone 500
            DrawerBackground= "#0D3322",   // Lichen 900
            DrawerText      = "#A8D9BC",   // Lichen 200
            AppbarBackground = "#0D3322",
            AppbarText       = "#A8D9BC",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Segoe UI", "Arial", "sans-serif"],
                FontSize = "0.875rem",
            },
            H6 = new H6Typography { FontSize = "1rem", FontWeight = "600" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "240px",
        },
    };
}
