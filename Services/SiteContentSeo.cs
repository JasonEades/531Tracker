using System.Net;
using System.Text.RegularExpressions;
using FiveThreeOneTracker.Models;

namespace FiveThreeOneTracker.Services;

public static partial class SiteContentSeo
{
    public static string ResolveBaseUrl(string? configuredBaseUrl, string fallbackBaseUrl)
    {
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var configured)
            && configured.Scheme is "http" or "https")
            return configured.GetLeftPart(UriPartial.Authority);

        if (Uri.TryCreate(fallbackBaseUrl, UriKind.Absolute, out var fallback)
            && fallback.Scheme is "http" or "https")
            return fallback.GetLeftPart(UriPartial.Authority);

        throw new InvalidOperationException("A valid public site base URL is required for SEO links.");
    }

    public static string GetDescription(SiteContent content)
    {
        var text = string.IsNullOrWhiteSpace(content.Summary)
            ? StripMarkdown(content.Body)
            : WebUtility.HtmlDecode(content.Summary);
        text = WhitespaceRegex().Replace(text, " ").Trim();
        if (text.Length <= 160) return text;

        var shortened = text[..160];
        var lastSpace = shortened.LastIndexOf(' ');
        if (lastSpace > 100) shortened = shortened[..lastSpace];
        return shortened.TrimEnd('.', ',', ';', ':', ' ') + "…";
    }

    public static string GetCanonicalUrl(string baseUrl, SiteContentType type, string slug)
    {
        var section = type == SiteContentType.BlogPost ? "blog" : "reviews";
        return $"{baseUrl.TrimEnd('/')}/{section}/{Uri.EscapeDataString(slug)}";
    }

    public static string GetImageUrl(string baseUrl, string imageUrl)
    {
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var absolute)
            ? absolute.AbsoluteUri
            : new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"), imageUrl).AbsoluteUri;
    }

    public static string BuildRobotsTxt(string baseUrl) =>
        $"User-agent: *{Environment.NewLine}" +
        $"Allow: /blog{Environment.NewLine}" +
        $"Allow: /reviews{Environment.NewLine}" +
        $"Allow: /fitness-tracker{Environment.NewLine}" +
        $"Disallow: /admin{Environment.NewLine}" +
        $"Disallow: /settings{Environment.NewLine}" +
        $"Disallow: /login{Environment.NewLine}" +
        $"Disallow: /logout{Environment.NewLine}" +
        $"Disallow: /cycles{Environment.NewLine}" +
        $"Disallow: /ppl{Environment.NewLine}" +
        $"Disallow: /workout{Environment.NewLine}" +
        $"Disallow: /sessions{Environment.NewLine}" +
        $"Disallow: /body{Environment.NewLine}" +
        $"Disallow: /protocol{Environment.NewLine}" +
        $"Disallow: /lifts{Environment.NewLine}" +
        $"Disallow: /accessories{Environment.NewLine}" +
        $"Disallow: /exercises{Environment.NewLine}" +
        $"Sitemap: {baseUrl.TrimEnd('/')}/sitemap.xml{Environment.NewLine}";

    private static string StripMarkdown(string markdown)
    {
        var html = SiteContentMarkdown.ToSafeHtml(markdown);
        return WebUtility.HtmlDecode(TagRegex().Replace(html, " "));
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
