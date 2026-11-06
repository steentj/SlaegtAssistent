using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyPersonLinks
{
    private readonly IReadOnlyDictionary<string, string> _paths;

    public BiographyPersonLinks(IEnumerable<KeyValuePair<string, string>> documents)
    {
        _paths = documents.GroupBy(item => item.Key, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => Path.GetFullPath(group.Single().Value), StringComparer.Ordinal);
    }

    public string? GetDocumentPath(string recordId) => _paths.GetValueOrDefault(recordId);

    public string? GetRelativePath(string sourceRecordId, string targetRecordId)
    {
        if (!_paths.TryGetValue(sourceRecordId, out var source) ||
            !_paths.TryGetValue(targetRecordId, out var target))
        {
            return null;
        }

        var relative = Path.GetRelativePath(Path.GetDirectoryName(source)!, target);
        if (Path.IsPathRooted(relative))
        {
            return null;
        }

        return string.Join('/', relative.Replace(Path.DirectorySeparatorChar, '/').Split('/').Select(Uri.EscapeDataString));
    }
}

internal sealed record PersonLinkMarkdown(string Text);
