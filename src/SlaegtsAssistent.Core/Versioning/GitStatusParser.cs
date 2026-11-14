using System.Collections.Generic;

namespace SlaegtsAssistent.Core.Versioning;

public static class GitStatusParser
{
    public static IReadOnlyList<WorkingTreeChange> Parse(string porcelainZ)
    {
        if (string.IsNullOrEmpty(porcelainZ))
        {
            return [];
        }

        var parts = porcelainZ.Split('\0');
        var changes = new List<WorkingTreeChange>();
        for (var i = 0; i < parts.Length; i++)
        {
            var entry = parts[i];
            if (string.IsNullOrEmpty(entry) || entry.Length < 3)
            {
                continue;
            }

            var x = entry[0];
            var y = entry[1];
            var path = entry[3..];
            if (!string.IsNullOrWhiteSpace(path))
            {
                var kind = ResolveKind(x, y);
                changes.Add(new WorkingTreeChange(path, kind));
            }

            if (x is 'R' or 'C' || y is 'R' or 'C')
            {
                i++;
            }
        }

        return changes
            .OrderBy(change => change.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static WorkingTreeChangeKind ResolveKind(char x, char y)
    {
        if (x == '?' && y == '?')
        {
            return WorkingTreeChangeKind.Ny;
        }

        if (x == 'A' || y == 'A')
        {
            return WorkingTreeChangeKind.Ny;
        }

        if (x == 'D' || y == 'D')
        {
            return WorkingTreeChangeKind.Slettet;
        }

        return WorkingTreeChangeKind.Aendret;
    }
}
