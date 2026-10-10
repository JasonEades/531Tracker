using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace FiveThreeOneTracker.Services;

public static class SiteContentMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    public static string ToSafeHtml(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        foreach (var link in document.Descendants().OfType<LinkInline>())
        {
            if (!IsAllowedUrl(link.Url))
                link.Url = null;
        }

        return Markdown.ToHtml(document, Pipeline);
    }

    private static bool IsAllowedUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);
    }
}
