namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyMediaResolution(
    string? RelativePath,
    string? Diagnostic,
    bool RequiresApproval,
    string? ResolvedPath)
{
    public IReadOnlyList<string> Candidates { get; init; } = [];
}

public sealed class BiographyMediaResolver
{
    private readonly Func<string, bool> _fileExists;
    private readonly Action<string> _assertReadable;

    public BiographyMediaResolver(
        Func<string, bool>? fileExists = null,
        Action<string>? assertReadable = null)
    {
        _fileExists = fileExists ?? File.Exists;
        _assertReadable = assertReadable ?? AssertReadable;
    }

    public BiographyMediaResolution Resolve(
        string? mediaPath,
        string? gedcomDirectory,
        string documentDirectory,
        string? mediaDirectory = null,
        MediaFileIndex? index = null)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            return new BiographyMediaResolution(null, "Mediehenvisningen har ingen filsti.", false, null);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(documentDirectory);
        var documentRoot = Path.GetFullPath(documentDirectory);
        var gedcomRoot = string.IsNullOrWhiteSpace(gedcomDirectory)
            ? documentRoot
            : Path.GetFullPath(gedcomDirectory);
        var mediaRoot = string.IsNullOrWhiteSpace(mediaDirectory) ? null : Path.GetFullPath(mediaDirectory);
        string resolved;
        try
        {
            var normalized = mediaPath.Replace('\\', '/');
            var windowsDrive = normalized.Length >= 2 && char.IsAsciiLetter(normalized[0]) && normalized[1] == ':';
            if (normalized.StartsWith("//", StringComparison.Ordinal) ||
                (!windowsDrive && Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && !uri.IsFile) ||
                normalized.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                return new BiographyMediaResolution(null, $"Mediestien `{mediaPath}` kan ikke åbnes lokalt.", true, null);
            }
            var foreignDrive = windowsDrive && !OperatingSystem.IsWindows();
            if (foreignDrive && (mediaRoot is null || index is null))
                return new BiographyMediaResolution(null, $"Mediestien `{mediaPath}` kan ikke åbnes lokalt.", true, null);
            var local = normalized.Replace('/', Path.DirectorySeparatorChar);
            resolved = foreignDrive ? mediaRoot! : Path.GetFullPath(Path.IsPathRooted(local) ? local : Path.Combine(mediaRoot ?? gedcomRoot, local));
            if (!foreignDrive && mediaRoot is not null && !Path.IsPathRooted(local) && !_fileExists(resolved))
            {
                var fallback = Path.GetFullPath(Path.Combine(gedcomRoot, local));
                if (_fileExists(fallback)) resolved = fallback;
            }
            if (mediaRoot is not null && index is not null && (foreignDrive || !_fileExists(resolved)))
            {
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!string.Equals(Path.TrimEndingDirectorySeparator(mediaRoot), Path.TrimEndingDirectorySeparator(index.Root), comparison))
                    return new BiographyMediaResolution(null, "Medieindekset tilhører en anden mappe. Opdatér indekset.", false, null);
                if (!foreignDrive && !Path.IsPathRooted(local) && !IsWithin(resolved, mediaRoot) && !IsWithin(resolved, gedcomRoot) && !IsWithin(resolved, documentRoot))
                    return new BiographyMediaResolution(null, $"Mediefilen `{mediaPath}` ligger uden for de tilladte lokale mapper.", true, null);
                var match = index.Find(mediaPath);
                if (match.Diagnostic is not null)
                    return new BiographyMediaResolution(null,
                        match.IsAmbiguous ? $"Mediehenvisningen `{mediaPath}` er tvetydig: {string.Join(", ", match.Candidates)}. Vælg en mere præcis mediemappe." : match.Diagnostic,
                        false, null) { Candidates = match.Candidates };
                if (match.Candidates.Count == 1) resolved = match.Candidates[0];
            }
            var roots = new[] { mediaRoot, gedcomRoot, documentRoot }.Where(root => root is not null)
                .Select(root => ResolvePhysicalPath(root!)).ToArray();
            var physical = ResolvePhysicalPath(resolved);
            if (!roots.Any(root => IsWithin(physical, root)))
            {
                return new BiographyMediaResolution(null,
                    $"Mediefilen `{mediaPath}` ligger uden for de tilladte lokale mapper.", true, resolved);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            return new BiographyMediaResolution(null, $"Mediestien `{mediaPath}` kan ikke kontrolleres.", true, null);
        }

        try
        {
            if (!_fileExists(resolved))
            {
                return new BiographyMediaResolution(
                    null,
                    $"Mediefilen `{mediaPath}` findes ikke.",
                    false,
                    resolved);
            }

            _assertReadable(resolved);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new BiographyMediaResolution(
                null,
                $"Mediefilen `{mediaPath}` kan ikke læses.",
                false,
                resolved);
        }

        var relative = Path.GetRelativePath(documentRoot, resolved).Replace('\\', '/');
        if (Path.IsPathRooted(relative))
        {
            return new BiographyMediaResolution(null, "Dokument og medie skal ligge på samme drev.", true, resolved);
        }
        return new BiographyMediaResolution(
            EncodeMarkdownPath(relative),
            null,
            false,
            resolved);
    }

    private static void AssertReadable(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    private static bool IsWithin(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path);
        return !Path.IsPathRooted(relative) &&
               !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
               !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static string ResolvePhysicalPath(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full) ?? throw new IOException("Stien mangler filsystemrod.");
        var current = root;
        foreach (var component in full[root.Length..].Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(current, component);
            FileSystemInfo info = Directory.Exists(candidate) ? new DirectoryInfo(candidate) : new FileInfo(candidate);
            current = info.Exists ? info.ResolveLinkTarget(true)?.FullName ?? candidate : candidate;
        }
        return Path.GetFullPath(current);
    }

    private static string EncodeMarkdownPath(string path) => string.Join(
        '/',
        path.Split('/').Select(segment => segment is "." or ".."
            ? segment
            : Uri.EscapeDataString(segment)));
}
