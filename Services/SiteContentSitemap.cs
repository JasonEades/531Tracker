using System.Xml.Linq;
using FiveThreeOneTracker.Models;

namespace FiveThreeOneTracker.Services;

public static class SiteContentSitemap
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static string Build(string baseUrl, IReadOnlyCollection<SiteContent> entries)
    {
        var urls = new List<(string Url, DateTime? LastModified)>
        {
            ($"{baseUrl.TrimEnd('/')}/fitness-tracker", null),
            ($"{baseUrl.TrimEnd('/')}/blog", null),
            ($"{baseUrl.TrimEnd('/')}/reviews", null),
            ($"{baseUrl.TrimEnd('/')}/privacy", null),
            ($"{baseUrl.TrimEnd('/')}/tos", null)
        };
        urls.AddRange(entries
            .Where(entry => entry.Status == SiteContentStatus.Published)
            .Select(entry => (
                Url: SiteContentSeo.GetCanonicalUrl(baseUrl, entry.Type, entry.Slug),
                LastModified: (DateTime?)entry.UpdatedAtUtc)));

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(SitemapNamespace + "urlset",
                urls.Select(item =>
                    new XElement(SitemapNamespace + "url",
                        new XElement(SitemapNamespace + "loc", item.Url),
                        item.LastModified is DateTime lastModified
                            ? new XElement(SitemapNamespace + "lastmod", lastModified.ToUniversalTime().ToString("yyyy-MM-dd"))
                            : null))));

        return document.ToString(SaveOptions.DisableFormatting);
    }
}
