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
                Background        = "#121211",
                BackgroundGray    = "#1A1A19",
                Surface           = "#1A1A19",
                DrawerBackground  = "#1A1A19",
                AppbarBackground  = "#1A1A19",

                // Brand / primary
                Primary           = "#F26A21",
                PrimaryContrastText = "#17110C",
                PrimaryDarken     = "#D95A16",
                PrimaryLighten    = "#FF7F3A",

                // Secondary
                Secondary         = "#9DB4C0",
                SecondaryContrastText = "#111415",

                // Tertiary / accent
                Tertiary          = "#E2B340",
                TertiaryContrastText = "#121211",

                // Semantic
                Success           = "#5BB974",
                SuccessContrastText = "#000000",
                Warning           = "#E2B340",
                WarningContrastText = "#000000",
                Error             = "#E5534B",
                ErrorContrastText = "#EEEBE5",
                Info              = "#9DB4C0",
                InfoContrastText  = "#121211",

                // Text
                TextPrimary       = "#EEEBE5",
                TextSecondary     = "#B9B4AA",
                TextDisabled      = "#8C877E",

                // Lines & overlays
                Divider           = "rgba(238,235,229,0.08)",
                DividerLight      = "rgba(238,235,229,0.04)",
                TableLines        = "rgba(238,235,229,0.08)",
                LinesDefault      = "rgba(238,235,229,0.08)",
                LinesInputs       = "rgba(238,235,229,0.14)",

                // Action states
                ActionDefault     = "#B9B4AA",
                ActionDisabled    = "#8C877E",
                ActionDisabledBackground = "rgba(238,235,229,0.06)",

                // Overlay / drawer
                OverlayDark       = "rgba(0,0,0,0.7)",
                DrawerText        = "#B9B4AA",
                DrawerIcon        = "#8C877E",
                AppbarText        = "#EEEBE5",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "400",
                    LineHeight = "1.6",
                    LetterSpacing = "normal",
                },
                H1 = new H1Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1.875rem",
                    FontWeight = "700",
                    LineHeight = "1.25",
                    LetterSpacing = "normal",
                },
                H2 = new H2Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1.5rem",
                    FontWeight = "700",
                    LineHeight = "1.25",
                    LetterSpacing = "normal",
                },
                H3 = new H3Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1.25rem",
                    FontWeight = "600",
                    LineHeight = "1.25",
                },
                H4 = new H4Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1.125rem",
                    FontWeight = "600",
                    LineHeight = "1.3",
                },
                H5 = new H5Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "600",
                    LineHeight = "1.4",
                },
                H6 = new H6Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "0.875rem",
                    FontWeight = "600",
                    LineHeight = "1.4",
                },
                Body1 = new Body1Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "400",
                    LineHeight = "1.6",
                },
                Body2 = new Body2Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "0.875rem",
                    FontWeight = "400",
                    LineHeight = "1.6",
                },
                Button = new ButtonTypography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "0.9375rem",
                    FontWeight = "600",
                    LineHeight = "1.5",
                    LetterSpacing = "normal",
                    TextTransform = "none",
                },
                Caption = new CaptionTypography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "0.75rem",
                    FontWeight = "400",
                    LineHeight = "1.4",
                },
                Overline = new OverlineTypography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "0.6875rem",
                    FontWeight = "700",
                    LineHeight = "1.4",
                    LetterSpacing = "0.06em",
                    TextTransform = "uppercase",
                },
                Subtitle1 = new Subtitle1Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "1rem",
                    FontWeight = "500",
                    LineHeight = "1.5",
                },
                Subtitle2 = new Subtitle2Typography
                {
                    FontFamily = ["Barlow", "system-ui", "-apple-system", "Segoe UI", "sans-serif"],
                    FontSize   = "0.875rem",
                    FontWeight = "500",
                    LineHeight = "1.5",
                },
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "6px",
                DrawerWidthLeft     = "220px",
                DrawerWidthRight    = "240px",
                AppbarHeight        = "56px",
            },
        };
    }
}
