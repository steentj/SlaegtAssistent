namespace SlaegtsAssistent.Core.Biography;

public sealed record MediaIndexMatch(IReadOnlyList<string> Candidates, string? Diagnostic)
{
    public bool IsAmbiguous => Candidates.Count > 1;
}

public sealed class MediaFileIndex
{
    public const int MaximumFiles = 100_000;
    private readonly string _root;
    public string Root => _root;
    private readonly StringComparer _comparer;
    private readonly IReadOnlyDictionary<string, string[]> _filesByName;
    private readonly string? _diagnostic;

    private MediaFileIndex(string root, StringComparer comparer, IReadOnlyList<string> files, string? diagnostic)
    {
        _root = root;
        _comparer = comparer;
        _filesByName = files.GroupBy(path => Path.GetFileName(path)!, comparer)
            .ToDictionary(group => group.Key, group => group.ToArray(), comparer);
        _diagnostic = diagnostic;
    }

    public static Task<MediaFileIndex> BuildAsync(string root, StringComparer comparer, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(comparer);
        var fullRoot = Path.GetFullPath(root);
        return Task.Run(() => Build(fullRoot, comparer, cancellationToken), cancellationToken);
    }

    private static MediaFileIndex Build(string root, StringComparer comparer, CancellationToken cancellationToken)
    {
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        try
        {
            while (pending.TryPop(out var directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    if ((entry.Attributes & FileAttributes.Directory) != 0)
                    {
                        pending.Push(entry.FullName);
                    }
                    else
                    {
                        files.Add(entry.FullName);
                        if (files.Count > MaximumFiles)
                            return new MediaFileIndex(root, comparer, [], "Mediemappen indeholder over 100.000 filer. Vælg en mindre mappe.");
                    }
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new MediaFileIndex(root, comparer, [], $"Mediemappen kunne ikke gennemgås fuldstændigt: {exception.Message}. Kontrollér adgang til mapperne.");
        }

        files.Sort(StringComparer.Ordinal);
        return new MediaFileIndex(root, comparer, files, null);
    }

    public MediaIndexMatch Find(string reference)
    {
        if (_diagnostic is not null) return new MediaIndexMatch([], _diagnostic);
        if (string.IsNullOrWhiteSpace(reference)) return new MediaIndexMatch([], "Mediehenvisningen har ingen filsti.");
        var referenceParts = reference.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (referenceParts.Length == 0) return new MediaIndexMatch([], "Mediehenvisningen har ingen filsti.");
        var best = 0;
        var candidates = new List<string>();
        if (!_filesByName.TryGetValue(referenceParts[^1], out var matchingFiles)) return new MediaIndexMatch([], null);
        foreach (var file in matchingFiles)
        {
            var parts = Path.GetRelativePath(_root, file).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            var length = 0;
            while (length < parts.Length && length < referenceParts.Length &&
                   _comparer.Equals(parts[^(length + 1)], referenceParts[^(length + 1)])) length++;
            if (length == 0 || length < best) continue;
            if (length > best) { best = length; candidates.Clear(); }
            candidates.Add(file);
        }
        return new MediaIndexMatch(candidates, candidates.Count > 1
            ? "Flere filer passer til mediehenvisningen. Vælg en mere præcis mediemappe."
            : null);
    }
}
