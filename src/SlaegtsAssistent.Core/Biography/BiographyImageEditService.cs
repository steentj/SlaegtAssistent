using System.Text;
using System.Text.RegularExpressions;

namespace SlaegtsAssistent.Core.Biography;

public enum BiographyImageWidth { FullPage, HalfPage, QuarterPage }

public sealed record BiographyImageSelection(string RelativePath, string AlternativeText, string? Caption,
    BiographyImageWidth Width = BiographyImageWidth.FullPage);
public sealed record EditorSelection(int Start, int End, long BufferRevision);
public sealed record BiographyImageEditResult(string Body, int CaretPosition, string? Diagnostic)
{
    public bool IsSuccess => Diagnostic is null;
}

public sealed class BiographyImageEditService
{
    public const string PortraitStartMarker = "<!-- SlaegtsAssistent:portrait:start -->";
    public const string PortraitEndMarker = "<!-- SlaegtsAssistent:portrait:end -->";
    private const string InvalidPortrait = "Topbilledets markører eller indhold er ugyldige. Ret blokken i Markdown, og prøv igen.";
    private const string InvalidGenerated = "Dokumentets genererede markører er ugyldige. Ret markørerne, og prøv igen.";
    private const string InvalidPosition = "Placér markøren i livsforløbet nederst i dokumentet.";
    private static readonly Regex PortraitImage = new(@"^!\[(?:\\.|[^\]\\])*\]\([^\r\n()]*\)$", RegexOptions.CultureInvariant);
    private static readonly Regex ImageSyntax = new(@"(?<!\\)!\[", RegexOptions.CultureInvariant);

    public BiographyImageEditResult SetPortrait(string body, BiographyImageSelection image)
    {
        ArgumentNullException.ThrowIfNull(body);
        var range = ReadPortrait(body);
        if (!range.Valid) return Failure(body, InvalidPortrait);
        var generated = ReadPair(body, BiographyGeneratedSectionMerger.StartMarker, BiographyGeneratedSectionMerger.EndMarker);
        if (!generated.Valid || (range.Exists && generated.Exists && range.End > generated.Start)) return Failure(body, InvalidGenerated);
        if (!TryImage(image, out var markdown)) return Failure(body, "Billedstien er ugyldig. Vælg et lokalt billede igen.");
        var newline = Newline(body);
        var content = ImageParagraph(markdown, image.Caption, newline);
        if (range.Exists)
        {
            var innerStart = range.Start + PortraitStartMarker.Length;
            var innerEnd = range.End - PortraitEndMarker.Length;
            var replacement = newline + content + newline;
            var updated = body[..innerStart] + replacement + body[innerEnd..];
            return new(updated, innerStart + replacement.Length, null);
        }
        var block = PortraitStartMarker + newline + content + newline + PortraitEndMarker + newline + newline;
        return new(block + body, block.Length, null);
    }

    public BiographyImageEditResult RemovePortrait(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var range = ReadPortrait(body);
        if (!range.Valid) return Failure(body, InvalidPortrait);
        var generated = ReadPair(body, BiographyGeneratedSectionMerger.StartMarker, BiographyGeneratedSectionMerger.EndMarker);
        if (!generated.Valid || (range.Exists && generated.Exists && range.End > generated.Start)) return Failure(body, InvalidGenerated);
        if (!range.Exists) return new(body, 0, null);
        var newline = Newline(body);
        var end = range.End;
        if (body.AsSpan(end).StartsWith(newline + newline, StringComparison.Ordinal)) end += newline.Length * 2;
        return new(body[..range.Start] + body[end..], range.Start, null);
    }

    public BiographyImageEditResult InsertInBiography(string body, EditorSelection selection, long bufferRevision, BiographyImageSelection image)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.BufferRevision != bufferRevision) return Failure(body, "Dokumentet er ændret. Åbn billedvælgeren igen.");
        if (selection.Start < 0 || selection.End < selection.Start || selection.End > body.Length ||
            SplitsSurrogate(body, selection.Start) || SplitsSurrogate(body, selection.End)) return Failure(body, "Markeringen er ugyldig. Vælg placeringen igen.");
        var portrait = ReadPortrait(body);
        if (!portrait.Valid) return Failure(body, InvalidPortrait);
        var generated = ReadPair(body, BiographyGeneratedSectionMerger.StartMarker, BiographyGeneratedSectionMerger.EndMarker);
        if (!generated.Valid || (portrait.Exists && generated.Exists && portrait.End > generated.Start)) return Failure(body, InvalidGenerated);
        if ((generated.Exists && selection.Start < generated.End) ||
            (portrait.Exists && selection.Start < portrait.End && selection.End > portrait.Start) ||
            (portrait.Exists && selection.Start == selection.End && selection.Start >= portrait.Start && selection.Start < portrait.End))
            return Failure(body, InvalidPosition);
        if (!TryImage(image, out var markdown)) return Failure(body, "Billedstien er ugyldig. Vælg et lokalt billede igen.");
        var newline = Newline(body);
        var before = body[..selection.Start];
        var after = body[selection.End..];
        var prefix = before.Length == 0 || before.EndsWith(newline + newline, StringComparison.Ordinal) ? ""
            : before.EndsWith(newline, StringComparison.Ordinal) ? newline : newline + newline;
        var suffix = after.Length == 0 || after.StartsWith(newline + newline, StringComparison.Ordinal) ? ""
            : after.StartsWith(newline, StringComparison.Ordinal) ? newline : newline + newline;
        var insertion = prefix + ImageParagraph(markdown, image.Caption, newline) + suffix;
        return new(before + insertion + after, before.Length + insertion.Length, null);
    }

    private static Pair ReadPortrait(string body)
    {
        var pair = ReadPair(body, PortraitStartMarker, PortraitEndMarker);
        if (!pair.Valid || !pair.Exists) return pair;
        if (pair.Start != 0) return pair with { Valid = false };
        var inner = body[(pair.Start + PortraitStartMarker.Length)..(pair.End - PortraitEndMarker.Length)];
        var lines = inner.Replace("\r\n", "\n", StringComparison.Ordinal).Trim('\n').Split('\n');
        if (lines.Length == 0 || !PortraitImage.IsMatch(lines[0]) || ImageSyntax.Matches(inner).Count != 1 ||
            (lines.Length > 1 && lines[1].Length != 0)) return pair with { Valid = false };
        return pair;
    }

    private static Pair ReadPair(string body, string startMarker, string endMarker)
    {
        var start = body.IndexOf(startMarker, StringComparison.Ordinal);
        var end = body.IndexOf(endMarker, StringComparison.Ordinal);
        if (start < 0 && end < 0) return new(true, false, 0, 0);
        if (start < 0 || end < start + startMarker.Length ||
            !IsMarkerLine(body, start, startMarker.Length) || !IsMarkerLine(body, end, endMarker.Length) ||
            body.IndexOf(startMarker, start + startMarker.Length, StringComparison.Ordinal) >= 0 ||
            body.IndexOf(endMarker, end + endMarker.Length, StringComparison.Ordinal) >= 0)
            return new(false, false, 0, 0);
        return new(true, true, start, end + endMarker.Length);
    }

    private static bool IsMarkerLine(string body, int start, int length)
    {
        var end = start + length;
        return (start == 0 || body[start - 1] == '\n') &&
            (end == body.Length || body[end] == '\n' ||
             (body[end] == '\r' && end + 1 < body.Length && body[end + 1] == '\n'));
    }

    private static bool TryImage(BiographyImageSelection image, out string markdown)
    {
        ArgumentNullException.ThrowIfNull(image);
        markdown = string.Empty;
        var path = image.RelativePath;
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.StartsWith('/') || path.StartsWith('\\') ||
            path.Contains('\\') || path.Any(char.IsWhiteSpace) || path.IndexOfAny(['(', ')', '<', '>']) >= 0 ||
            Uri.TryCreate(path, UriKind.Absolute, out _)) return false;
        var title = image.Width switch
        {
            BiographyImageWidth.FullPage => string.Empty,
            BiographyImageWidth.HalfPage => " \"SlaegtsAssistent:width=50\"",
            BiographyImageWidth.QuarterPage => " \"SlaegtsAssistent:width=25\"",
            _ => null,
        };
        if (title is null) return false;
        markdown = $"![{Escape(image.AlternativeText)}]({path}{title})";
        return true;
    }

    private static string ImageParagraph(string markdown, string? caption, string newline) =>
        string.IsNullOrWhiteSpace(caption) ? markdown : markdown + newline + newline + Escape(caption);

    private static string Escape(string? text)
    {
        var result = new StringBuilder();
        foreach (var character in text ?? string.Empty)
        {
            if (char.IsControl(character)) { result.Append(' '); continue; }
            if (character == '<') { result.Append("&lt;"); continue; }
            if (character == '>') { result.Append("&gt;"); continue; }
            if (character == '&') { result.Append("&amp;"); continue; }
            if ("\\`*_{}[]()#+-.!|".Contains(character, StringComparison.Ordinal)) result.Append('\\');
            result.Append(character);
        }
        return result.ToString();
    }

    private static bool SplitsSurrogate(string body, int position) => position > 0 && position < body.Length &&
        char.IsHighSurrogate(body[position - 1]) && char.IsLowSurrogate(body[position]);
    private static string Newline(string body) => body.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    private static BiographyImageEditResult Failure(string body, string diagnostic) => new(body, 0, diagnostic);
    private sealed record Pair(bool Valid, bool Exists, int Start, int End);
}
