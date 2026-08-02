namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyGeneratedSectionCandidate(
    string Content,
    bool RequiresMigration,
    bool ChangesExistingDocument);

public static class BiographyGeneratedSectionMerger
{
    public const string StartMarker = "<!-- SlaegtsAssistent:generated:start -->";
    public const string EndMarker = "<!-- SlaegtsAssistent:generated:end -->";

    public static string Wrap(string generatedContent)
    {
        ArgumentNullException.ThrowIfNull(generatedContent);
        return $"{StartMarker}\n{generatedContent.TrimEnd('\r', '\n')}\n{EndMarker}\n";
    }

    public static BiographyGeneratedSectionCandidate CreateCandidate(
        string existingContent,
        string generatedContent)
    {
        ArgumentNullException.ThrowIfNull(existingContent);
        ArgumentNullException.ThrowIfNull(generatedContent);

        var wrapped = ExtractWrappedSection(generatedContent) ?? Wrap(generatedContent);
        var start = existingContent.IndexOf(StartMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            var separator = existingContent.EndsWith('\n') ? "\n" : "\n\n";
            return new BiographyGeneratedSectionCandidate(
                existingContent + separator + wrapped,
                true,
                true);
        }

        var end = existingContent.IndexOf(EndMarker, start + StartMarker.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            var separator = existingContent.EndsWith('\n') ? "\n" : "\n\n";
            return new BiographyGeneratedSectionCandidate(
                existingContent + separator + wrapped,
                true,
                true);
        }

        end += EndMarker.Length;
        var preservedBiography = ExtractBiographySection(existingContent[start..end]);
        var suffix = existingContent[end..];
        if (preservedBiography is not null &&
            !suffix.Contains("## Biografi", StringComparison.Ordinal))
        {
            suffix = $"\n{preservedBiography}\n{suffix.TrimStart('\r', '\n')}";
        }

        var candidate = existingContent[..start] + wrapped + suffix;
        var existingSection = existingContent[start..end].TrimEnd('\r', '\n');
        var candidateSection = wrapped.TrimEnd('\r', '\n');
        return new BiographyGeneratedSectionCandidate(
            candidate,
            false,
            !string.Equals(existingSection, candidateSection, StringComparison.Ordinal));
    }

    public static string ApplyApprovedCandidate(
        BiographyGeneratedSectionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.Content;
    }

    private static string? ExtractWrappedSection(string content)
    {
        var start = content.IndexOf(StartMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var end = content.IndexOf(EndMarker, start + StartMarker.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        end += EndMarker.Length;
        return content[start..end] + "\n";
    }

    private static string? ExtractBiographySection(string content)
    {
        var heading = content.IndexOf("## Biografi", StringComparison.Ordinal);
        if (heading < 0)
        {
            return null;
        }

        var section = content[heading..];
        var marker = section.IndexOf(EndMarker, StringComparison.Ordinal);
        if (marker >= 0)
        {
            section = section[..marker];
        }

        return section.Trim('\r', '\n');
    }
}
