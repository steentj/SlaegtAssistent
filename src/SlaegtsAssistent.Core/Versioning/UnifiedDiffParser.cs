namespace SlaegtsAssistent.Core.Versioning;

public static class UnifiedDiffParser
{
    public static IReadOnlyList<DiffFile> Parse(string diffText)
    {
        if (string.IsNullOrEmpty(diffText))
        {
            return [];
        }

        var lines = diffText.Split('\n');
        var files = new List<DiffFile>();
        Section? current = null;
        foreach (var line in lines)
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                if (current is not null)
                {
                    files.Add(current.ToDiffFile());
                }

                current = new Section();
                continue;
            }

            if (current is null)
            {
                continue;
            }

            current.ProcessLine(line);
        }

        if (current is not null)
        {
            files.Add(current.ToDiffFile());
        }

        return files;
    }

    private sealed class Section
    {
        private readonly List<DiffLine> _lines = [];
        private string? _plusPath;
        private string? _minusPath;
        private string? _renameTo;
        private bool _newFile;
        private bool _deletedFile;
        private bool _inHunk;

        public void ProcessLine(string line)
        {
            if (line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                _plusPath = NormalizeGitPath(line[4..]);
                return;
            }

            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                _minusPath = NormalizeGitPath(line[4..]);
                return;
            }

            if (line.StartsWith("rename to ", StringComparison.Ordinal))
            {
                _renameTo = NormalizeGitPath(line["rename to ".Length..]);
                return;
            }

            if (line.StartsWith("new file mode ", StringComparison.Ordinal))
            {
                _newFile = true;
                return;
            }

            if (line.StartsWith("deleted file mode ", StringComparison.Ordinal))
            {
                _deletedFile = true;
                return;
            }

            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                _inHunk = true;
                _lines.Add(new DiffLine(DiffLineKind.HunkHeader, TrimEndingCr(line)));
                return;
            }

            if (!_inHunk || line.StartsWith(@"\ No newline at end of file", StringComparison.Ordinal))
            {
                return;
            }

            if (line.Length == 0)
            {
                return;
            }

            var prefix = line[0];
            var text = TrimEndingCr(line[1..]);
            _lines.Add(prefix switch
            {
                '+' => new DiffLine(DiffLineKind.Added, text),
                '-' => new DiffLine(DiffLineKind.Removed, text),
                ' ' => new DiffLine(DiffLineKind.Context, text),
                _ => new DiffLine(DiffLineKind.Context, TrimEndingCr(line)),
            });
        }

        public DiffFile ToDiffFile()
        {
            var path = _renameTo
                       ?? (_plusPath is "/dev/null" ? null : _plusPath)
                       ?? (_minusPath is "/dev/null" ? null : _minusPath)
                       ?? string.Empty;
            var kind = _newFile
                ? WorkingTreeChangeKind.Ny
                : _deletedFile
                    ? WorkingTreeChangeKind.Slettet
                    : WorkingTreeChangeKind.Aendret;
            return new DiffFile(path ?? string.Empty, kind, _lines.ToArray());
        }

        private static string? NormalizeGitPath(string value)
        {
            var path = TrimEndingCr(value);
            if (path.StartsWith("a/", StringComparison.Ordinal) || path.StartsWith("b/", StringComparison.Ordinal))
            {
                return path[2..];
            }

            return path;
        }

        private static string TrimEndingCr(string value) => value.TrimEnd('\r');
    }
}
