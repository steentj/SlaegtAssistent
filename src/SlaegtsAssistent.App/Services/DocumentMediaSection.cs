using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

internal sealed record DocumentMediaSection(int InsertAt, IReadOnlyList<MediaCatalogItem> Items)
{
    internal const string NavigationScheme = "slaegt-media";

    public static DocumentMediaSection? Find(
        BiographyDocument document, string ownerId, string documentPath, string? templatePath)
    {
        var markdown = document.Body;
        var parsed = Markdown.Parse(markdown, SafeMarkdownPreviewService.Pipeline);
        var markers = Regex.Matches(markdown,
                "^" + Regex.Escape(BiographyGeneratedSectionMerger.StartMarker) + @"\r?$|^" +
                Regex.Escape(BiographyGeneratedSectionMerger.EndMarker) + @"\r?$", RegexOptions.Multiline)
            .Where(marker => !parsed.Descendants<CodeBlock>().Any(
                block => marker.Index >= block.Span.Start && marker.Index <= block.Span.End))
            .ToArray();
        var legacy = markers.Length == 0;
        if (!legacy && (markers.Length != 2 ||
                        !markers[0].Value.StartsWith(BiographyGeneratedSectionMerger.StartMarker, StringComparison.Ordinal) ||
                        !markers[1].Value.StartsWith(BiographyGeneratedSectionMerger.EndMarker, StringComparison.Ordinal)))
            return null;

        var start = legacy ? 0 : markers[0].Index + markers[0].Length;
        var end = legacy ? markdown.Length : markers[1].Index;
        var sectionName = FindHeadingName(document.Metadata, templatePath);
        if (string.IsNullOrWhiteSpace(sectionName) ||
            document.Metadata is { } metadata && metadata.RecordId != ownerId) return null;

        var headings = parsed.OfType<HeadingBlock>().Where(
            heading => heading.Span.Start >= start && heading.Span.End < end).ToArray();
        var matches = headings.Where(heading => string.Equals(
            InlineText(heading.Inline).Trim(), sectionName.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) return null;
        var match = matches[0];
        var bodyStart = match.Span.End + 1;
        // Slutpositionen omfatter også understregningen ved en Setext-overskrift.
        var lineEnd = markdown.IndexOf('\n', bodyStart);
        var insertAt = lineEnd < 0 ? markdown.Length : lineEnd + 1;
        var nextHeading = headings.FirstOrDefault(
            heading => heading.Span.Start > match.Span.Start && heading.Level <= match.Level);
        var bodyEnd = nextHeading?.Span.Start ?? end;

        var approved = new MediaCatalogService().Build([new(ownerId, document, null)]);
        var hasApprovedList = document.Metadata?.SyncBaseline is { Version: BiographySyncBaseline.CurrentVersion } baseline &&
                              baseline.Approved.Version == CanonicalBiographySnapshot.CurrentVersion &&
                              baseline.Approved.Person.RecordId == ownerId;
        var items = hasApprovedList ? approved : ReadLegacyLinks(parsed, ownerId, documentPath, bodyStart, bodyEnd);
        return items.Count > 0 ? new DocumentMediaSection(insertAt, items) : null;
    }

    private static string? FindHeadingName(BiographyDocumentMetadata? metadata, string? templatePath)
    {
        if (templatePath is null || metadata?.TemplateHash is null) return "Medier";
        try
        {
            var model = new BiographyTemplateLoader().LoadEditorModel(templatePath);
            var source = new BiographyTemplateEditorCompiler().Compile(model);
            if (BiographyTemplateIdentity.ComputeHash(source) != metadata.TemplateHash) return "Medier";
            var media = model.Blocks.SingleOrDefault(block => block.Type == BiographyTemplateBlockType.Media && block.IsVisible);
            return media?.Heading is { } heading
                ? InlineText(Markdown.Parse("## " + heading, SafeMarkdownPreviewService.Pipeline)
                    .OfType<HeadingBlock>().FirstOrDefault()?.Inline)
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BiographyTemplateException)
        {
            return "Medier";
        }
    }

    private static IReadOnlyList<MediaCatalogItem> ReadLegacyLinks(
        MarkdownDocument parsed, string ownerId, string documentPath, int start, int end)
    {
        var items = new List<MediaCatalogItem>();
        foreach (var link in parsed.Descendants<LinkInline>())
        {
            if (link.Span.Start < start || link.Span.End >= end || string.IsNullOrWhiteSpace(link.Url))
                continue;
            var path = ResolveLegacyLink(link.Url, Path.GetDirectoryName(Path.GetFullPath(documentPath))!);
            if (path is null) continue;
            items.Add(new MediaCatalogItem("link:" + link.Span.Start, ownerId, null, InlineText(link), path));
        }
        return items.DistinctBy(item => (item.Reference, item.Title)).ToArray();
    }

    private static string? ResolveLegacyLink(string url, string documentDirectory)
    {
        try
        {
            var relative = Uri.UnescapeDataString(url.Split(['?', '#'], 2)[0]).Replace('\\', '/');
            if (relative.Length == 0 || relative.StartsWith('/') || relative.Contains(':') ||
                !Path.HasExtension(relative)) return null;
            return Path.GetFullPath(relative, documentDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or UriFormatException)
        {
            return null;
        }
    }

    private static string InlineText(ContainerInline? container)
    {
        if (container is null) return string.Empty;
        var text = new StringBuilder();
        foreach (var inline in container)
        {
            if (inline is LiteralInline literal) text.Append(literal.Content.ToString());
            else if (inline is CodeInline code) text.Append(code.Content);
            else if (inline is HtmlEntityInline entity) text.Append(entity.Transcoded.ToString());
            else if (inline is ContainerInline nested) text.Append(InlineText(nested));
        }
        return text.ToString();
    }
}

internal sealed record DocumentMediaCard(
    MediaCatalogItem Item, Uri? Navigation, string? Thumbnail = null, string? Status = null)
{
    public string Name => !string.IsNullOrWhiteSpace(Item.Title) ? Item.Title.Trim()
        : !string.IsNullOrWhiteSpace(Item.FileName) ? Item.FileName : "Medie uden navn";

    public static string Render(IReadOnlyList<DocumentMediaCard> cards)
    {
        var html = new StringBuilder("<ul class=\"document-media\" aria-label=\"Medier\">");
        foreach (var card in cards)
        {
            html.Append("<li>");
            if (card.Navigation is not null)
                html.Append("<a href=\"").Append(WebUtility.HtmlEncode(card.Navigation.AbsoluteUri)).Append("\">");
            if (card.Thumbnail is not null)
                html.Append("<img width=\"160\" height=\"120\" alt=\"\" src=\"data:image/png;base64,")
                    .Append(WebUtility.HtmlEncode(card.Thumbnail)).Append("\">");
            html.Append("<span>").Append(WebUtility.HtmlEncode(card.Name)).Append("</span>");
            if (card.Navigation is not null) html.Append("</a>");
            if (card.Status is not null)
                html.Append("<small>").Append(WebUtility.HtmlEncode(card.Status)).Append("</small>");
            html.Append("</li>");
        }
        return html.Append("</ul>").ToString();
    }
}
