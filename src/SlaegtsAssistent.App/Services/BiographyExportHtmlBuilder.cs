using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Extensions.Footnotes;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Services;

public sealed record BiographyExportHtmlResult(string Html, IReadOnlyList<string> Warnings)
{
    public IReadOnlyList<string> SourceHashes { get; init; } = [];
}

public sealed class BiographyExportHtmlBuilder
{
    public static string ComputeFingerprint(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(html)));
    }

    public static string ComputeFingerprint(BiographyExportHtmlResult result) =>
        ComputeFingerprint(result.Html + "\0" + string.Join("", result.SourceHashes));

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables().UseEmphasisExtras().UseFootnotes().UseAutoIdentifiers().Build();

    public BiographyExportHtmlResult Build(IReadOnlyList<BiographyExportItem> items, BiographyExportMode mode, ExportResourceCollector? resources = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        var warnings = new List<string>();
        var sourceHashes = new List<string>();
        var ordered = items.OrderBy(x => x.DisplayName, StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), false))
            .ThenBy(x => x.RecordId, StringComparer.Ordinal).ToArray();
        if (ordered.Select(x => x.RecordId).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            throw new ArgumentException("Person-id'er skal være entydige.", nameof(items));
        var bookmarks = ordered.Select(x => x.RecordId).OrderBy(x => x, StringComparer.Ordinal)
            .Select((id, index) => (id, bookmark: $"person-{index + 1:000000}"))
            .ToDictionary(x => x.id, x => x.bookmark, StringComparer.Ordinal);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.Write("<!doctype html><html lang=\"da\"><head><meta charset=\"utf-8\"><style>@page{size:A4;margin:20mm;@bottom-center{content:counter(page)}}body{font-size:11pt;line-height:1.35;overflow-wrap:anywhere}h1,h2,h3,h4,h5,h6{break-after:avoid}img{max-width:100%}thead{display:table-header-group}.chapter+.chapter{break-before:page}</style></head><body>");
        if (mode == BiographyExportMode.Combined) writer.Write("<header><h1>Slægtsbiografier</h1></header>");
        foreach (var item in ordered)
        {
            var document = Markdown.Parse(item.Body, Pipeline);
            SuppressInlineSvgContents(document);
            var anchors = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var heading in document.Descendants<HeadingBlock>())
            {
                var attributes = heading.GetAttributes();
                if (string.IsNullOrEmpty(attributes.Id)) continue;
                var original = attributes.Id;
                attributes.Id = bookmarks[item.RecordId] + "-" + original;
                anchors.TryAdd(original, attributes.Id);
            }
            var blocks = document.ToArray();
            var portraitEnd = blocks.Length > 0 && IsMarker(blocks[0], BiographyImageEditService.PortraitStartMarker)
                ? Array.FindIndex(blocks, block => IsMarker(block, BiographyImageEditService.PortraitEndMarker)) : -1;
            var first = blocks.Skip(portraitEnd + 1).FirstOrDefault(block => !IsInternalMarker(block)) as HeadingBlock;
            var sameHeading = first is { Level: 1 } && string.Equals(NormalizeName(PlainText(first.Inline)), NormalizeName(item.DisplayName), StringComparison.Ordinal);
            foreach (var heading in document.Descendants<HeadingBlock>())
                if (sameHeading)
                {
                    if (!ReferenceEquals(heading, first)) heading.Level = Math.Max(2, heading.Level);
                }
                else heading.Level = Math.Min(6, heading.Level + 1);
            writer.Write("<section class=\"chapter\">");
            if (!sameHeading) { writer.Write("<h1 id=\""); writer.Write(bookmarks[item.RecordId]); writer.Write("\">"); writer.Write(System.Net.WebUtility.HtmlEncode(item.DisplayName)); writer.Write("</h1>"); }
            var renderer = new HtmlRenderer(writer);
            Pipeline.Setup(renderer);
            if (sameHeading) renderer.ObjectRenderers.Replace<HeadingRenderer>(new ExportHeadingRenderer(first!, bookmarks[item.RecordId]));
            renderer.ObjectRenderers.Replace<LinkInlineRenderer>(new ExportLinkRenderer(item, ordered, bookmarks, mode, warnings, resources, anchors, sourceHashes));
            renderer.ObjectRenderers.Replace<HtmlFootnoteGroupRenderer>(new ExportFootnoteGroupRenderer(bookmarks[item.RecordId]));
            renderer.ObjectRenderers.Replace<HtmlFootnoteLinkRenderer>(new ExportFootnoteLinkRenderer(bookmarks[item.RecordId]));
            renderer.ObjectRenderers.Replace<HtmlBlockRenderer>(new ExportHtmlBlockRenderer(warnings));
            renderer.ObjectRenderers.Replace<HtmlInlineRenderer>(new ExportHtmlInlineRenderer(warnings));
            renderer.Render(document);
            writer.Write("</section>");
        }
        writer.Write("</body></html>");
        return new(writer.ToString(), warnings.Distinct(StringComparer.Ordinal).ToArray()) { SourceHashes = sourceHashes.ToArray() };
    }

    private static string NormalizeName(string name) =>
        string.Join(' ', name.Normalize(System.Text.NormalizationForm.FormC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool IsMarker(Block block, string marker) => block is HtmlBlock html &&
        string.Equals(html.Lines.ToString().Trim(), marker, StringComparison.Ordinal);

    private static bool IsInternalMarker(Block block) => block is HtmlBlock html &&
        html.Lines.ToString().Trim() is BiographyGeneratedSectionMerger.StartMarker or BiographyGeneratedSectionMerger.EndMarker
            or BiographyImageEditService.PortraitStartMarker or BiographyImageEditService.PortraitEndMarker;

    private static string PlainText(ContainerInline? inline) => inline is null ? "" :
        string.Concat(inline.Descendants<LiteralInline>().Select(x => x.Content.ToString()));

    private sealed class ExportLinkRenderer(BiographyExportItem source, IReadOnlyList<BiographyExportItem> items,
        IReadOnlyDictionary<string, string> bookmarks, BiographyExportMode mode, List<string> warnings, ExportResourceCollector? resources, IReadOnlyDictionary<string, string> anchors, List<string> sourceHashes) : HtmlObjectRenderer<LinkInline>
    {
        protected override void Write(HtmlRenderer renderer, LinkInline link)
        {
            if (link.IsImage)
            {
                var widthPercent = link.Title switch
                {
                    "SlaegtsAssistent:width=50" => 50,
                    "SlaegtsAssistent:width=25" => 25,
                    _ => 100,
                };
                var image = resources?.Collect(link.Url ?? "", source.SourcePath, widthPercent, sourceHashes.Add);
                if (image is not null)
                {
                    renderer.Write("<img src=\""); renderer.WriteEscape(image); renderer.Write("\" alt=\"");
                    renderer.WriteEscape(PlainText(link)); renderer.Write("\" />");
                    return;
                }
                warnings.Add("Et billede kunne ikke medtages i eksporten.");
                renderer.Write("<span>"); renderer.WriteChildren(link); renderer.Write(" (Billede ikke medtaget)</span>");
                return;
            }
            var url = link.GetDynamicUrl?.Invoke() ?? link.Url ?? "";
            string? destination = null;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") destination = uri.AbsoluteUri;
            else if (url.StartsWith('#') && anchors.TryGetValue(Uri.UnescapeDataString(url[1..]), out var anchor)) destination = "#" + anchor;
            else if (!Uri.TryCreate(url, UriKind.Absolute, out _) && !url.StartsWith('#'))
            {
                try
                {
                    var relative = Uri.UnescapeDataString(url.Split(['?', '#'], 2)[0]);
                    if (Path.IsPathRooted(relative) || relative.Contains(':') || relative.Contains('\\')) throw new ArgumentException();
                    var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(source.SourcePath))!, relative));
                    var match = items.Where(x => string.Equals(Path.GetFullPath(x.SourcePath), path, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).ToArray();
                    if (mode == BiographyExportMode.Combined && match.Length == 1) destination = "#" + bookmarks[match[0].RecordId];
                }
                catch (Exception ex) when (ex is ArgumentException or IOException or UriFormatException or NotSupportedException) { }
            }
            if (destination is null) warnings.Add("Et link uden et tilladt mål blev udeladt.");
            else { renderer.Write("<a href=\""); renderer.WriteEscape(destination); renderer.Write("\">"); }
            renderer.WriteChildren(link);
            if (destination is not null) renderer.Write("</a>");
        }
    }

    private sealed class ExportHeadingRenderer(HeadingBlock chapterHeading, string bookmark) : HtmlObjectRenderer<HeadingBlock>
    {
        protected override void Write(HtmlRenderer renderer, HeadingBlock heading)
        {
            renderer.Write("<h"); renderer.Write(heading.Level.ToString(CultureInfo.InvariantCulture));
            var id = heading.TryGetAttributes()?.Id;
            if (!string.IsNullOrEmpty(id)) { renderer.Write(" id=\""); renderer.WriteEscape(id); renderer.Write("\""); }
            renderer.Write(">");
            if (ReferenceEquals(heading, chapterHeading))
            {
                renderer.Write("<span id=\""); renderer.Write(bookmark); renderer.Write("\"></span>");
            }
            renderer.WriteLeafInline(heading);
            renderer.Write("</h"); renderer.Write(heading.Level.ToString(CultureInfo.InvariantCulture)); renderer.WriteLine(">");
        }
    }

    private sealed class ExportFootnoteGroupRenderer(string chapter) : HtmlObjectRenderer<FootnoteGroup>
    {
        protected override void Write(HtmlRenderer renderer, FootnoteGroup group)
        {
            renderer.Write("<div class=\"footnotes\"><hr /><ol>");
            foreach (var footnote in group.OfType<Footnote>())
            {
                renderer.Write("<li><span id=\""); renderer.Write(chapter); renderer.Write("-fn-");
                renderer.Write(footnote.Order.ToString(CultureInfo.InvariantCulture)); renderer.Write("\"></span>");
                renderer.WriteChildren(footnote);
                renderer.Write("</li>");
            }
            renderer.Write("</ol></div>");
        }
    }

    private sealed class ExportFootnoteLinkRenderer(string chapter) : HtmlObjectRenderer<FootnoteLink>
    {
        protected override void Write(HtmlRenderer renderer, FootnoteLink link)
        {
            var number = link.Footnote.Order.ToString(CultureInfo.InvariantCulture);
            var reference = chapter + "-fnref-" + number + "-" + link.Index.ToString(CultureInfo.InvariantCulture);
            var note = chapter + "-fn-" + number;
            if (link.IsBackLink)
            {
                renderer.Write("<a href=\"#"); renderer.Write(reference); renderer.Write("\" class=\"footnote-back-ref\">&#8617;</a>");
            }
            else
            {
                renderer.Write("<a id=\""); renderer.Write(reference); renderer.Write("\" href=\"#"); renderer.Write(note);
                renderer.Write("\" class=\"footnote-ref\"><sup>"); renderer.Write(number); renderer.Write("</sup></a>");
            }
        }
    }

    private static void SuppressInlineSvgContents(MarkdownDocument document)
    {
        foreach (var container in document.Descendants<LeafBlock>().Select(block => block.Inline).Where(inline => inline is not null).SelectMany(inline => new[] { inline! }.Concat(inline!.Descendants<ContainerInline>())).ToArray())
        {
            for (var current = container.FirstChild; current is not null; current = current.NextSibling)
            {
                if (current is not HtmlInline html || !IsRawTag(html.Tag.AsSpan().TrimStart(), "svg") || html.Tag.AsSpan().TrimEnd().EndsWith("/>", StringComparison.Ordinal)) continue;
                var depth = 1;
                Inline? end = null;
                for (var candidate = current.NextSibling; candidate is not null; candidate = candidate.NextSibling)
                {
                    if (candidate is not HtmlInline nested) continue;
                    if (IsRawTag(nested.Tag.AsSpan().TrimStart(), "svg") && !nested.Tag.AsSpan().TrimEnd().EndsWith("/>", StringComparison.Ordinal)) depth++;
                    else if (IsClosingSvg(nested.Tag) && --depth == 0) { end = candidate; break; }
                }
                if (end is null) continue;
                var after = end.NextSibling;
                for (var remove = current.NextSibling; remove != after;)
                {
                    var next = remove!.NextSibling;
                    remove.Remove();
                    remove = next;
                }
            }
        }
    }

    private static bool IsClosingSvg(string tag)
    {
        var text = tag.AsSpan().TrimStart();
        return text.Length >= 6 && text[..5].Equals("</svg", StringComparison.OrdinalIgnoreCase) &&
            (char.IsWhiteSpace(text[5]) || text[5] == '>');
    }

    private static bool IsRawImage(string html)
    {
        var text = html.AsSpan().TrimStart();
        return IsRawTag(text, "img") || IsRawTag(text, "svg");
    }

    private static bool IsRawTag(ReadOnlySpan<char> text, string tag) =>
        text.Length > tag.Length + 1 && text[0] == '<' && text.Slice(1, tag.Length).Equals(tag, StringComparison.OrdinalIgnoreCase) &&
        (char.IsWhiteSpace(text[tag.Length + 1]) || text[tag.Length + 1] is '>' or '/');

    private static string TrailingRawImageText(string html)
    {
        var trimmed = html.TrimStart();
        var end = -1;
        if (IsRawTag(trimmed.AsSpan(), "svg"))
        {
            var closing = trimmed.IndexOf("</svg", StringComparison.OrdinalIgnoreCase);
            if (closing >= 0) end = trimmed.IndexOf('>', closing);
        }
        if (end < 0)
        {
            char quote = '\0';
            for (var i = 0; i < trimmed.Length; i++)
            {
                var character = trimmed[i];
                if (quote != '\0') { if (character == quote) quote = '\0'; }
                else if (character is '\"' or '\'') quote = character;
                else if (character == '>') { end = i; break; }
            }
        }
        return end >= 0 && end + 1 < trimmed.Length ? trimmed[(end + 1)..] : "";
    }

    private static void WriteRawImagePlaceholder(HtmlRenderer renderer, string html, List<string> warnings)
    {
        warnings.Add("Et råt HTML-billede blev ikke medtaget i eksporten.");
        var alt = "";
        var position = 1;
        while (position < html.Length && !char.IsWhiteSpace(html[position]) && html[position] != '>') position++;
        while (position < html.Length)
        {
            while (position < html.Length && char.IsWhiteSpace(html[position])) position++;
            var start = position;
            while (position < html.Length && (char.IsLetterOrDigit(html[position]) || html[position] is '-' or '_' or ':')) position++;
            if (start == position) break;
            var name = html[start..position];
            while (position < html.Length && char.IsWhiteSpace(html[position])) position++;
            if (position >= html.Length || html[position] != '=') continue;
            position++;
            while (position < html.Length && char.IsWhiteSpace(html[position])) position++;
            if (position >= html.Length) break;
            var quote = html[position] is '\"' or '\'' ? html[position++] : '\0';
            var valueStart = position;
            while (position < html.Length && (quote == '\0' ? !char.IsWhiteSpace(html[position]) && html[position] != '>' : html[position] != quote)) position++;
            if (name.Equals("alt", StringComparison.OrdinalIgnoreCase)) alt = System.Net.WebUtility.HtmlDecode(html[valueStart..position]);
            if (quote != '\0' && position < html.Length) position++;
        }
        renderer.Write("<span>"); renderer.WriteEscape(alt);
        renderer.Write(alt.Length == 0 ? "Billede ikke medtaget</span>" : " (Billede ikke medtaget)</span>");
    }

    private sealed class ExportHtmlBlockRenderer(List<string> warnings) : HtmlObjectRenderer<HtmlBlock>
    {
        protected override void Write(HtmlRenderer renderer, HtmlBlock block)
        {
            var text = block.Lines.ToString().Trim();
            if (text is BiographyGeneratedSectionMerger.StartMarker or BiographyGeneratedSectionMerger.EndMarker or BiographyImageEditService.PortraitStartMarker or BiographyImageEditService.PortraitEndMarker) return;
            if (IsRawImage(text))
            {
                WriteRawImagePlaceholder(renderer, text, warnings);
                var trailing = TrailingRawImageText(text);
                if (!string.IsNullOrWhiteSpace(trailing)) { renderer.Write("<pre>"); renderer.WriteEscape(trailing); renderer.Write("</pre>"); }
                return;
            }
            warnings.Add("Rå HTML vises som tekst i eksporten.");
            renderer.Write("<pre>"); renderer.WriteEscape(text); renderer.Write("</pre>");
        }
    }
    private sealed class ExportHtmlInlineRenderer(List<string> warnings) : HtmlObjectRenderer<HtmlInline>
    {
        protected override void Write(HtmlRenderer renderer, HtmlInline inline)
        {
            if (inline.Tag is BiographyGeneratedSectionMerger.StartMarker or BiographyGeneratedSectionMerger.EndMarker or BiographyImageEditService.PortraitStartMarker or BiographyImageEditService.PortraitEndMarker) return;
            if (IsRawImage(inline.Tag)) { WriteRawImagePlaceholder(renderer, inline.Tag, warnings); return; }
            warnings.Add("Rå HTML vises som tekst i eksporten.");
            renderer.WriteEscape(inline.Tag);
        }
    }
}
