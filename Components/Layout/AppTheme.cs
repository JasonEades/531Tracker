using MudBlazor;

namespace FiveThreeOneTracker.Components.Layout;

public static class AppTheme
{
    public static MudTheme Create()
    {
        return new MudTheme
        {
            PaletteDark = new PaletteDark
            {
                // Backgrounds
                Background        = "#090B0F",
                BackgroundGray    = "#14171F",
                Surface           = "#14171F",
                DrawerBackground  = "#14171F",
                AppbarBackground  = "#14171F",

                // Brand / primary
                Primary           = "#5E7CFF",
                PrimaryContrastText = "#FFFFFF",
                PrimaryDarken     = "#4A68EB",
                PrimaryLighten    = "#7690FF",

                // Secondary
                Secondary         = "#6EE7FF",
                SecondaryContrastText = "#090B0F",

                // Tertiary / accent
                Tertiary          = "#FFB547",
                TertiaryContrastText = "#090B0F",

                // Semantic
                Success           = "#3DDC84",
                SuccessContrastText = "#000000",
                Warning           = "#FFB547",
                WarningContrastText = "#000000",
                Error             = "#FF5A5F",
                ErrorContrastText = "#FFFFFF",
                Info              = "#6EE7FF",
                InfoContrastText  = "#090B0F",

                // Text
                TextPrimary       = "#FFFFFF",
                TextSecondary     = "#B8C0CC",
                TextDisabled      = "#7D8795",

                // Lines & overlays
                Divider           = "rgba(255,255,255,0.08)",
                DividerLight      = "rgba(255,255,255,0.04)",
                TableLines        = "rgba(255,255,255,0.08)",
                LinesDefault      = "rgba(255,255,255,0.08)",
                LinesInputs       = "rgba(255,255,255,0.14)",

                // Action states
                ActionDefault     = "#B8C0CC",
                ActionDisabled    = "#7D8795",
                ActionDisabledBackground = "rgba(255,255,255,0.06)",

                // Overlay / drawer
                OverlayDark       = "rgba(0,0,0,0.7)",
                DrawerText        = "#B8C0CC",
                DrawerIcon        = "#7D8795",
                AppbarText        = "#FFFFFF",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "400",
                    LineHeight = "1.6",
                    LetterSpacing = "normal",
                },
                H1 = new H1Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1.875rem",
                    FontWeight = "700",
                    LineHeight = "1.25",
                    LetterSpacing = "-0.01em",
                },
                H2 = new H2Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1.5rem",
                    FontWeight = "700",
                    LineHeight = "1.25",
                    LetterSpacing = "-0.01em",
                },
                H3 = new H3Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1.25rem",
                    FontWeight = "600",
                    LineHeight = "1.25",
                },
                H4 = new H4Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1.125rem",
                    FontWeight = "600",
                    LineHeight = "1.3",
                },
                H5 = new H5Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "600",
                    LineHeight = "1.4",
                },
                H6 = new H6Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "0.875rem",
                    FontWeight = "600",
                    LineHeight = "1.4",
                },
                Body1 = new Body1Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "400",
                    LineHeight = "1.6",
                },
                Body2 = new Body2Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "0.875rem",
                    FontWeight = "400",
                    LineHeight = "1.6",
                },
                Button = new ButtonTypography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "0.9375rem",
                    FontWeight = "600",
                    LineHeight = "1.5",
                    LetterSpacing = "-0.01em",
                    TextTransform = "none",
                },
                Caption = new CaptionTypography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "0.75rem",
                    FontWeight = "400",
                    LineHeight = "1.4",
                },
                Overline = new OverlineTypography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "0.6875rem",
                    FontWeight = "700",
                    LineHeight = "1.4",
                    LetterSpacing = "0.06em",
                    TextTransform = "uppercase",
                },
                Subtitle1 = new Subtitle1Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "500",
                    LineHeight = "1.5",
                },
                Subtitle2 = new Subtitle2Typography
                {
                    FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                    FontSize   = "0.875rem",
                    FontWeight = "500",
                    LineHeight = "1.5",
                },
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "14px",
                DrawerWidthLeft     = "220px",
                DrawerWidthRight    = "240px",
                AppbarHeight        = "56px",
            },
        };
    }
}
