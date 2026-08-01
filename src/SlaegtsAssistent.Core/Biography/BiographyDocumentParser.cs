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
        metadata = metadata with
        {
            Facts = ExtractVisibleFacts(body, metadata.Facts),
        };
        return new BiographyDocument(metadata, body, true);
    }

    public static BiographyFactsSnapshot ExtractVisibleFacts(
        string body,
        BiographyFactsSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(baseline);

        var facts = baseline;
        var representedFields = baseline.RepresentedFields is null
            ? null
            : new HashSet<string>(baseline.RepresentedFields, StringComparer.Ordinal);
        if (TryReadHeading(body, out var headingName))
        {
            facts = facts with { FullName = headingName };
            representedFields?.Add("Navn");
        }

        if (TryReadFact(body, "Født", out var birthValue))
        {
            var (date, place) = SplitDateAndPlace(birthValue);
            facts = facts with { BirthDate = date, BirthPlace = place };
            representedFields?.UnionWith(["Fødselsdato", "Fødested"]);
        }

        if (TryReadFact(body, "Død", out var deathValue))
        {
            var (date, place) = SplitDateAndPlace(deathValue);
            facts = facts with { DeathDate = date, DeathPlace = place };
            representedFields?.UnionWith(["Dødsdato", "Dødssted"]);
        }

        if (TryReadFact(body, "Forældre", out var parentsValue))
        {
            facts = facts with { ParentDisplayText = parentsValue };
            representedFields?.Add("Forældre");
        }

        return facts with { RepresentedFields = representedFields };
    }

    private static bool TryReadHeading(string body, out string value)
    {
        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                value = line[2..].Trim();
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryReadFact(string body, string label, out string value)
    {
        var inFactsSection = false;
        var prefix = $"- **{label}:**";

        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Equals("## Fakta", StringComparison.Ordinal))
            {
                inFactsSection = true;
                continue;
            }

            if (inFactsSection &&
                line.StartsWith("## ", StringComparison.Ordinal) &&
                !line.Equals("## Fakta", StringComparison.Ordinal))
            {
                break;
            }

            if (inFactsSection && line.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = line[prefix.Length..].Trim();
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static (string? Date, string? Place) SplitDateAndPlace(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        if (value.StartsWith("i ", StringComparison.Ordinal))
        {
            return (null, value[2..].Trim());
        }

        var separator = value.IndexOf(" i ", StringComparison.Ordinal);
        return separator < 0
            ? (value, null)
            : (value[..separator].Trim(), value[(separator + 3)..].Trim());
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
