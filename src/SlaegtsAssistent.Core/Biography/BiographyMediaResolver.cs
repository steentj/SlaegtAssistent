namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyMediaResolution(
    string? RelativePath,
    string? Diagnostic,
    bool RequiresApproval,
    string? ResolvedPath);

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
        string documentDirectory)
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
        string resolved;
        try
        {
            resolved = Path.GetFullPath(
                Path.IsPathRooted(mediaPath)
                    ? mediaPath
                    : Path.Combine(gedcomRoot, mediaPath.Replace('\\', Path.DirectorySeparatorChar)));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new BiographyMediaResolution(
                null,
                $"Mediestien `{mediaPath}` er ugyldig.",
                true,
                null);
        }

        if (!IsWithin(resolved, gedcomRoot) && !IsWithin(resolved, documentRoot))
        {
            return new BiographyMediaResolution(
                null,
                $"Mediefilen `{mediaPath}` ligger uden for de tilladte lokale mapper og kræver brugerens godkendelse.",
                true,
                resolved);
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

    private static string EncodeMarkdownPath(string path) => string.Join(
        '/',
        path.Split('/').Select(segment => segment is "." or ".."
            ? segment
            : Uri.EscapeDataString(segment)));
}
