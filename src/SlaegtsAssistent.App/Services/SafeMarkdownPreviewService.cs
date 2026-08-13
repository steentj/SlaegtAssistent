using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;

namespace SlaegtsAssistent.App.Services;

public sealed record SafeMarkdownPreviewResult(
    string Html,
    string HtmlDocument,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<Uri> ExternalLinks,
    int NetworkRequestAttempts);

public sealed class SafeMarkdownPreviewService
{
    public const string ContentSecurityPolicy =
        "default-src 'none'; img-src data:; style-src 'unsafe-inline'; " +
        "font-src 'none'; connect-src 'none'; frame-src 'none'; media-src 'none'; " +
        "object-src 'none'; script-src 'none'; base-uri 'none'; form-action 'none'";

    private const long MaximumImageBytes = 20 * 1024 * 1024;
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras()
        .DisableHtml()
        .Build();
    private static readonly Regex ImageRegex = new(
        "<img\\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new(
        "<a\\b[^>]*href=\"(?<href>[^\"]*)\"[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SourceRegex = new(
        "\\bsrc=\"(?<src>[^\"]*)\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex RawHtmlRegex = new(
        "<\\/?[a-z][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly IReadOnlyDictionary<string, string> ImageMimeTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".bmp"] = "image/bmp",
        };

    public SafeMarkdownPreviewResult Render(
        string markdown,
        string documentPath,
        IReadOnlyCollection<string>? allowedLocalRoots = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(documentPath))
            ?? throw new ArgumentException("Dokumentstien mangler en mappe.", nameof(documentPath));
        var roots = (allowedLocalRoots ?? [])
            .Append(documentDirectory)
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(Path.GetFullPath)
            .Distinct(PathComparer)
            .ToArray();
        var diagnostics = new List<string>();
        var externalLinks = new List<Uri>();
        if (RawHtmlRegex.IsMatch(markdown))
        {
            diagnostics.Add("Rå HTML vises som tekst og udføres ikke i previewet.");
        }

        var html = Markdown.ToHtml(markdown, Pipeline);

        html = ImageRegex.Replace(html, match => ResolveImage(
            match.Value,
            documentDirectory,
            roots,
            diagnostics));
        html = LinkRegex.Replace(html, match => ResolveLink(match, externalLinks, diagnostics));

        if (diagnostics.Count > 0)
        {
            html += "<aside class=\"preview-warning\" role=\"status\">" +
                    "Indhold blev blokeret af hensyn til privatliv og sikkerhed:<ul>" +
                    string.Concat(diagnostics.Distinct(StringComparer.Ordinal)
                        .Select(message => $"<li>{WebUtility.HtmlEncode(message)}</li>")) +
                    "</ul></aside>\n";
        }

        return new SafeMarkdownPreviewResult(
            html,
            CreateDocument(html),
            diagnostics.Distinct(StringComparer.Ordinal).ToArray(),
            externalLinks.DistinctBy(uri => uri.AbsoluteUri, StringComparer.Ordinal).ToArray(),
            NetworkRequestAttempts: 0);
    }

    public SafeMarkdownPreviewResult RenderHelp(string markdown)
    {
        var syntheticPath = Path.Combine(Path.GetTempPath(), "slaegtsassistent-hjaelp.md");
        return Render(markdown, syntheticPath, []);
    }

    private static string ResolveImage(
        string imageTag,
        string documentDirectory,
        IReadOnlyList<string> allowedRoots,
        ICollection<string> diagnostics)
    {
        var sourceMatch = SourceRegex.Match(imageTag);
        if (!sourceMatch.Success)
        {
            diagnostics.Add("Et billede uden en gyldig kilde blev udeladt.");
            return Warning("Billede blev blokeret");
        }

        var source = WebUtility.HtmlDecode(sourceMatch.Groups["src"].Value);
        if (Uri.TryCreate(source, UriKind.Absolute, out var absolute) &&
            !absolute.IsFile)
        {
            diagnostics.Add($"Eksternt billede blev blokeret: {SafeDestination(absolute)}");
            return Warning("Eksternt billede blev blokeret");
        }

        string resolved;
        try
        {
            var localSource = absolute?.IsFile == true
                ? absolute.LocalPath
                : Uri.UnescapeDataString(source.Split(['?', '#'], 2)[0]);
            resolved = Path.GetFullPath(
                Path.IsPathRooted(localSource)
                    ? localSource
                    : Path.Combine(documentDirectory, localSource.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or UriFormatException)
        {
            diagnostics.Add("En ugyldig lokal billedsti blev blokeret.");
            return Warning("Lokalt billede blev blokeret");
        }

        string physicalPath;
        bool isWithinAllowedRoot;
        try
        {
            physicalPath = ResolvePhysicalPath(resolved);
            isWithinAllowedRoot = allowedRoots.Any(root =>
                IsWithin(physicalPath, ResolvePhysicalPath(root)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add("En lokal billedsti kunne ikke kontrolleres sikkert.");
            return Warning("Lokalt billede blev blokeret");
        }

        if (!isWithinAllowedRoot)
        {
            diagnostics.Add("Et lokalt billede uden for de tilladte mapper blev blokeret.");
            return Warning("Lokalt billede blev blokeret");
        }

        var extension = Path.GetExtension(resolved);
        if (!ImageMimeTypes.TryGetValue(extension, out var mimeType))
        {
            diagnostics.Add("Et lokalt billede med et ikke-tilladt filformat blev blokeret.");
            return Warning("Lokalt billede blev blokeret");
        }

        try
        {
            var info = new FileInfo(physicalPath);
            if (!info.Exists)
            {
                diagnostics.Add("En lokal billedfil blev ikke fundet.");
                return Warning("Lokalt billede mangler");
            }

            if (info.Length > MaximumImageBytes)
            {
                diagnostics.Add("En lokal billedfil var større end den tilladte previewgrænse på 20 MB.");
                return Warning("Lokalt billede var for stort");
            }

            var data = Convert.ToBase64String(File.ReadAllBytes(physicalPath));
            return SourceRegex.Replace(imageTag, $"src=\"data:{mimeType};base64,{data}\"", 1);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add("En lokal billedfil kunne ikke læses.");
            return Warning("Lokalt billede kunne ikke læses");
        }
    }

    private static string ResolveLink(
        Match match,
        ICollection<Uri> externalLinks,
        ICollection<string> diagnostics)
    {
        var href = WebUtility.HtmlDecode(match.Groups["href"].Value);
        if (href.StartsWith('#'))
        {
            return match.Value;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out var uri) &&
            uri.Scheme is "https" or "http")
        {
            externalLinks.Add(uri);
            return match.Value[..^1] + " data-preview-external=\"true\" rel=\"noreferrer noopener\">";
        }

        diagnostics.Add("Et link med et lokalt, aktivt eller ikke-understøttet URL-skema blev blokeret.");
        return Regex.Replace(
            match.Value,
            "\\s*href=\"[^\"]*\"",
            "",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string Warning(string text) =>
        $"<span class=\"preview-warning\">{WebUtility.HtmlEncode(text)}</span>";

    private static string SafeDestination(Uri uri) =>
        uri.GetLeftPart(UriPartial.Path);

    private static string CreateDocument(string html) =>
        "<!doctype html><html><head><meta charset=\"utf-8\">" +
        $"<meta http-equiv=\"Content-Security-Policy\" content=\"{ContentSecurityPolicy}\">" +
        "<meta name=\"referrer\" content=\"no-referrer\">" +
        "<style>body{font-family:system-ui,sans-serif;line-height:1.55;margin:24px;color:#23313a;background:#FFFFFF;}" +
        "h1,h2,h3{line-height:1.2;color:#174a5b;}code,pre{background:#edf2f3;padding:2px 4px;}" +
        "table{border-collapse:collapse;margin:12px 0;}th,td{border:1px solid #9aaeb5;padding:6px 10px;text-align:left;}" +
        "blockquote{border-left:4px solid #6e9eaa;margin-left:0;padding-left:12px;color:#4d626a;}" +
        ".preview-warning{display:block;margin:8px 0;padding:8px 10px;border-left:4px solid #B7791F;background:#FFF8E6;color:#6B4E16;}" +
        "img{max-width:100%;height:auto;}</style></head><body>" + html + "</body></html>";

    private static bool IsWithin(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path);
        return !Path.IsPathRooted(relative) &&
               !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
               !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static string ResolvePhysicalPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)
            ?? throw new IOException("Stien har ingen filsystemrod.");
        var current = root;
        var components = fullPath[root.Length..].Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var component in components)
        {
            var candidate = Path.Combine(current, component);
            FileSystemInfo info = Directory.Exists(candidate)
                ? new DirectoryInfo(candidate)
                : new FileInfo(candidate);
            var target = info.ResolveLinkTarget(returnFinalTarget: true);
            current = target?.FullName ?? candidate;
        }

        return Path.GetFullPath(current);
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}

public sealed record PreviewNavigationDecision(
    bool AllowInPreview,
    bool RequiresConfirmation,
    string DisplayDestination);

public static class PreviewNavigationPolicy
{
    public static PreviewNavigationDecision Evaluate(Uri destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Scheme == "about")
        {
            return new PreviewNavigationDecision(true, false, destination.AbsoluteUri);
        }

        if (destination.Scheme is "https" or "http")
        {
            return new PreviewNavigationDecision(false, true, destination.AbsoluteUri);
        }

        return new PreviewNavigationDecision(false, false, destination.AbsoluteUri);
    }

    public static bool AllowsResource(Uri destination) => destination.Scheme is "data" or "about";
}
