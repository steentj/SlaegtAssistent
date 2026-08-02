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

        var wrapped = Wrap(generatedContent);
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
        var candidate = existingContent[..start] + wrapped + existingContent[end..];
        return new BiographyGeneratedSectionCandidate(
            candidate,
            false,
            !string.Equals(existingContent, candidate, StringComparison.Ordinal));
    }

    public static string ApplyApprovedCandidate(
        BiographyGeneratedSectionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.Content;
    }
}
