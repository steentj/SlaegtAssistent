using System.Globalization;

namespace SlaegtsAssistent.Core.Versioning;

public static class GitLogParser
{
    public static VersionHistory Parse(string output, int maxEntries)
    {
        if (maxEntries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            return new VersionHistory([], false);
        }

        var entries = new List<VersionEntry>();
        foreach (var rawEntry in output.Split('\x1e'))
        {
            var entry = rawEntry.TrimStart('\n', '\r');
            if (string.IsNullOrEmpty(entry))
            {
                continue;
            }

            var fields = entry.Split('\x1f');
            if (fields.Length != 3)
            {
                continue;
            }

            entries.Add(new VersionEntry(
                fields[0],
                DateTimeOffset.Parse(fields[1], CultureInfo.InvariantCulture),
                fields[2]));
        }

        var hasMore = entries.Count > maxEntries;
        if (hasMore)
        {
            entries = entries.Take(maxEntries).ToList();
        }

        return new VersionHistory(entries, hasMore);
    }
}
