using System.Text.Json;

namespace SlaegtsAssistent.Core.Biography;

public static class BiographyDocumentParser
{
    public static BiographyDocument Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var prefix = $"---{newline}";
        if (!content.StartsWith(prefix, StringComparison.Ordinal))
        {
            return new BiographyDocument(null, content, false);
        }

        var marker = $"{newline}---{newline}";
        var markerEnd = content.IndexOf(marker, prefix.Length, StringComparison.Ordinal);
        if (markerEnd < 0)
        {
            return new BiographyDocument(null, content, false);
        }

        var frontMatter = content[prefix.Length..markerEnd];
        var body = content[(markerEnd + marker.Length)..];
        var metadata = ParseMetadata(frontMatter);
        return new BiographyDocument(metadata, body, true);
    }

    private static BiographyDocumentMetadata ParseMetadata(string frontMatter)
    {
        var values = frontMatter
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .ToDictionary(
                line => line.StartsWith("  ", StringComparison.Ordinal)
                    ? line[2..].Split(':', 2)[0]
                    : line.Split(':', 2)[0],
                line => line.Split(':', 2).Length == 2 ? line.Split(':', 2)[1].Trim() : string.Empty,
                StringComparer.Ordinal);

        var formatVersion = ParseInt(values, "formatVersion");
        var recordId = ParseString(values, "recordId")
            ?? throw new FormatException("Dokumentets record-id mangler.");
        var facts = new BiographyFactsSnapshot(
            ParseString(values, "fullName"),
            ParseString(values, "sex"),
            ParseString(values, "birthDate"),
            ParseString(values, "birthPlace"),
            ParseString(values, "deathDate"),
            ParseString(values, "deathPlace"),
            ParseStringArray(values, "parentRecordIds"));

        return new BiographyDocumentMetadata(
            formatVersion,
            recordId,
            ParseString(values, "displayName"),
            facts);
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : throw new FormatException($"Dokumentets {key} er ugyldig.");
    }

    private static string? ParseString(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is "null" or "")
        {
            return null;
        }

        return JsonSerializer.Deserialize<string>(value)
            ?? throw new FormatException($"Dokumentets {key} er ugyldig.");
    }

    private static IReadOnlyList<string> ParseStringArray(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return [];
        }

        return JsonSerializer.Deserialize<string[]>(value)
            ?? throw new FormatException($"Dokumentets {key} er ugyldigt.");
    }
}
