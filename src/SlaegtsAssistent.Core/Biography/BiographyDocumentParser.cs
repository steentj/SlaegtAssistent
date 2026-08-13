using System.Text.Json;

namespace SlaegtsAssistent.Core.Biography;

public static class BiographyDocumentParser
{
    public const int CurrentFormatVersion = 3;

    public static BiographyDocument Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var result = ParseSafely(content);
        if (result.IsSuccess)
        {
            return result.Document!;
        }

        throw new FormatException(result.Diagnostic?.Message ?? "Dokumentets metadata er ugyldige.");
    }

    public static BiographyDocumentParseResult ParseSafely(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var newline = content.StartsWith("---\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var prefix = $"---{newline}";
        if (!content.StartsWith(prefix, StringComparison.Ordinal))
        {
            return Success(new BiographyDocument(null, content, false));
        }

        var marker = $"{newline}---{newline}";
        var markerEnd = content.IndexOf(marker, prefix.Length, StringComparison.Ordinal);
        if (markerEnd < 0)
        {
            return Failure(
                BiographyDocumentErrorCategory.MalformedFrontMatter,
                "Dokumentets frontmatter mangler en afsluttende '---'-markør.",
                "Ret eller fjern den ufuldstændige frontmatter, og indlæs arbejdsområdet igen.");
        }

        var frontMatter = content[prefix.Length..markerEnd];
        var body = content[(markerEnd + marker.Length)..];
        try
        {
            var values = ParseValues(frontMatter);
            var formatVersion = ParseRequiredInt(values, "formatVersion");
            if (formatVersion is < 0 or > CurrentFormatVersion)
            {
                return Failure(
                    BiographyDocumentErrorCategory.UnsupportedFormatVersion,
                    $"Dokumentets formatversion {formatVersion} understøttes ikke.",
                    "Bevar filen uændret, og åbn den med en version af appen, der understøtter formatet.");
            }

            var metadata = ParseMetadata(values, formatVersion);
            metadata = metadata with
            {
                Facts = ExtractVisibleFacts(body, metadata.Facts),
            };
            var document = new BiographyDocument(metadata, body, true);
            if (formatVersion == CurrentFormatVersion)
            {
                return Success(document);
            }

            var migratedMetadata = metadata with { FormatVersion = CurrentFormatVersion };
            return new BiographyDocumentParseResult(
                document,
                null,
                RequiresMigration: true,
                BiographyDocumentSerializer.Serialize(migratedMetadata, body));
        }
        catch (MetadataParseException exception)
        {
            return Failure(exception.Category, exception.Message, exception.NextAction);
        }
        catch (JsonException exception)
        {
            return Failure(
                BiographyDocumentErrorCategory.InvalidValue,
                $"Dokumentets metadata indeholder en ugyldig JSON-værdi: {exception.Message}",
                "Ret værdien i frontmatter, og indlæs arbejdsområdet igen.");
        }
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

    private static IReadOnlyDictionary<string, string> ParseValues(string frontMatter)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in frontMatter.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var normalizedLine = line.StartsWith("  ", StringComparison.Ordinal) ? line[2..] : line;
            var parts = normalizedLine.Split(':', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new MetadataParseException(
                    BiographyDocumentErrorCategory.MalformedFrontMatter,
                    $"Frontmatter-linjen '{line}' har ikke formatet 'nøgle: værdi'.",
                    "Ret linjen, og indlæs arbejdsområdet igen.");
            }

            var key = parts[0];
            if (!values.TryAdd(key, parts[1].Trim()))
            {
                throw new MetadataParseException(
                    BiographyDocumentErrorCategory.DuplicateKey,
                    $"Dokumentets nøgle '{key}' forekommer mere end én gang.",
                    $"Ret den dublerede nøgle '{key}', og indlæs arbejdsområdet igen.");
            }
        }

        return values;
    }

    private static BiographyDocumentMetadata ParseMetadata(
        IReadOnlyDictionary<string, string> values,
        int formatVersion)
    {
        var recordId = ParseRequiredString(values, "recordId");
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
            facts)
        {
            GedcomBaselineHash = ParseString(values, "gedcomBaselineHash"),
            TemplateHash = ParseString(values, "templateHash"),
            SyncBaseline = ParseSyncBaseline(values),
        };
    }

    private static BiographySyncBaseline? ParseSyncBaseline(IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue("syncBaseline", out var value) || value is "null" or "")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BiographySyncBaseline>(value)
                ?? throw InvalidValue("syncBaseline");
        }
        catch (JsonException)
        {
            throw InvalidValue("syncBaseline");
        }
    }

    private static int ParseRequiredInt(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw MissingField(key);
        }

        if (!int.TryParse(value, out var parsed))
        {
            throw InvalidValue(key);
        }

        return parsed;
    }

    private static string ParseRequiredString(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is "null" or "")
        {
            throw MissingField(key);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string>(value);
            return string.IsNullOrWhiteSpace(parsed) ? throw MissingField(key) : parsed;
        }
        catch (JsonException)
        {
            throw InvalidValue(key);
        }
    }

    private static string? ParseString(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is "null" or "")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string>(value);
        }
        catch (JsonException)
        {
            throw InvalidValue(key);
        }
    }

    private static IReadOnlyList<string> ParseStringArray(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(value)
                ?? throw InvalidValue(key);
        }
        catch (JsonException)
        {
            throw InvalidValue(key);
        }
    }

    private static BiographyDocumentParseResult Success(BiographyDocument document)
    {
        return new BiographyDocumentParseResult(document, null);
    }

    private static BiographyDocumentParseResult Failure(
        BiographyDocumentErrorCategory category,
        string message,
        string nextAction)
    {
        return new BiographyDocumentParseResult(
            null,
            new BiographyDocumentDiagnostic(category, message, nextAction));
    }

    private static MetadataParseException MissingField(string key)
    {
        return new MetadataParseException(
            BiographyDocumentErrorCategory.MissingRequiredField,
            $"Dokumentets obligatoriske felt '{key}' mangler.",
            $"Tilføj en gyldig værdi for '{key}', og indlæs arbejdsområdet igen.");
    }

    private static MetadataParseException InvalidValue(string key)
    {
        return new MetadataParseException(
            BiographyDocumentErrorCategory.InvalidValue,
            $"Dokumentets værdi for '{key}' er ugyldig.",
            $"Ret værdien for '{key}', og indlæs arbejdsområdet igen.");
    }

    private sealed class MetadataParseException(
        BiographyDocumentErrorCategory category,
        string message,
        string nextAction) : Exception(message)
    {
        public BiographyDocumentErrorCategory Category { get; } = category;

        public string NextAction { get; } = nextAction;
    }
}
