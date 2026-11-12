using System;
using System.IO;
using System.Linq;

namespace SlaegtsAssistent.App.Services;

internal sealed record LocalExportTool(string Executable, string LibraryDirectory, string FontConfigFile);

internal static class LocalExportToolLocator
{
    internal static LocalExportTool? Find(string baseDirectory, string? developmentExecutable = null)
    {
        if (!Path.IsPathFullyQualified(baseDirectory)) throw new ArgumentException("Programmets mappe skal være absolut.", nameof(baseDirectory));
        var name = OperatingSystem.IsWindows() ? "slaegt-export.exe" : "slaegt-export";
        var bundled = FromExecutable(Path.Combine(baseDirectory, "tools", "export", "slaegt-export", name));
        if (bundled is not null) return bundled;
#if DEBUG
        if (!string.IsNullOrWhiteSpace(developmentExecutable) && Path.IsPathFullyQualified(developmentExecutable))
            return FromExecutable(developmentExecutable);
#endif
        return null;
    }

    private static LocalExportTool? FromExecutable(string executable)
    {
        var libraries = Path.Combine(Path.GetDirectoryName(executable)!, "_internal");
        var fontConfig = Path.Combine(libraries, "fonts.conf");
        var fonts = Path.Combine(libraries, "fonts");
        if (!File.Exists(executable) || !Directory.Exists(libraries) || !File.Exists(fontConfig) || !Directory.Exists(fonts)) return null;
        try
        {
            if (!Directory.EnumerateFiles(fonts).Any(path => Path.GetExtension(path).Equals(".ttf", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".otf", StringComparison.OrdinalIgnoreCase))) return null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        return new(executable, libraries, fontConfig);
    }
}
