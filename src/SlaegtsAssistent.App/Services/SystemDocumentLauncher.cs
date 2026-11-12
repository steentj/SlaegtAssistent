using System;
using System.Diagnostics;
using System.IO;

namespace SlaegtsAssistent.App.Services;

public interface ISystemDocumentLauncher
{
    void OpenPdf(string path);
}

public sealed class SystemDocumentLauncher : ISystemDocumentLauncher
{
    public void OpenPdf(string path)
    {
        if (!Path.IsPathFullyQualified(path) || !File.Exists(path) || !string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("PDF-filen til udskrift er ugyldig.", nameof(path));
        using var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        if (process is null) throw new InvalidOperationException("PDF-fremviseren kunne ikke åbnes.");
    }
}

public static class PrintDocumentStore
{
    private const string Prefix = "slaegtsassistent-udskrift-";
    private const string Marker = ".slaegtsassistent-udskrift";
    private const string MarkerContent = "SlaegtsAssistent udskrift v1";

    public static string Save(byte[] pdf)
    {
        if (pdf.Length < 5 || pdf.Length > 100 * 1024 * 1024 || !pdf.AsSpan().StartsWith("%PDF-"u8))
            throw new ArgumentException("PDF-filen til udskrift er ugyldig.", nameof(pdf));
        var directory = Path.Combine(Path.GetTempPath(), Prefix + Guid.NewGuid().ToString("N"));
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(directory);
        else Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            File.WriteAllText(Path.Combine(directory, Marker), MarkerContent);
            var path = Path.Combine(directory, "graf.pdf");
            new AtomicFileWriter().WriteBytes(path, pdf);
            return path;
        }
        catch
        {
            try { Directory.Delete(directory, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    public static void Cleanup(string temporaryRoot, DateTime utcNow)
    {
        try
        {
            foreach (var directory in new DirectoryInfo(temporaryRoot).EnumerateDirectories(Prefix + "*"))
            {
                try
                {
                    if (directory.LinkTarget is not null || !Guid.TryParseExact(directory.Name[Prefix.Length..], "N", out _)) continue;
                    var marker = new FileInfo(Path.Combine(directory.FullName, Marker));
                    if (!marker.Exists || marker.LinkTarget is not null || marker.Length > 128 || marker.LastWriteTimeUtc > utcNow.AddDays(-1)) continue;
                    if (File.ReadAllText(marker.FullName) != MarkerContent) continue;
                    Directory.Delete(directory.FullName, true);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
