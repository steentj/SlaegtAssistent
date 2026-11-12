using System;
using System.IO;
using System.Text;

namespace SlaegtsAssistent.App.Services;

internal sealed class ExportJobDirectory : IDisposable
{
    private const string Prefix = "slaegtsassistent-export-";
    private const string MarkerName = ".slaegtsassistent-job";
    private const string MarkerContent = "SlaegtsAssistent eksportjob v1";
    private readonly FileStream _marker;
    public string Path { get; }

    public ExportJobDirectory(string jobId)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Prefix + jobId);
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(Path);
        else Directory.CreateDirectory(Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        FileStream? marker = null;
        try
        {
            marker = new FileStream(System.IO.Path.Combine(Path, MarkerName), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            var bytes = Encoding.UTF8.GetBytes(MarkerContent);
            marker.Write(bytes);
            marker.Flush();
            _marker = marker;
        }
        catch
        {
            marker?.Dispose();
            try { Directory.Delete(Path, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    public void Dispose()
    {
        _marker.Dispose();
        try { Directory.Delete(Path, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    internal static void CleanupAbandoned(string temporaryRoot, DateTime utcNow)
    {
        try
        {
            foreach (var directory in new DirectoryInfo(temporaryRoot).EnumerateDirectories(Prefix + "*"))
            {
                try
                {
                    if (directory.LinkTarget is not null || !Guid.TryParseExact(directory.Name[Prefix.Length..], "N", out _)) continue;
                    var marker = new FileInfo(System.IO.Path.Combine(directory.FullName, MarkerName));
                    if (!marker.Exists || marker.LinkTarget is not null || marker.LastWriteTimeUtc > utcNow.AddDays(-1) || marker.Length > 128) continue;
                    using (var stream = new FileStream(marker.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        if (reader.ReadToEnd() != MarkerContent) continue;
                    }
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
