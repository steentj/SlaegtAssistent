using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public sealed class FileSystemGedcomSnapshotStore : IGedcomSnapshotStore
{
    private const int CurrentFormatVersion = 1;
    private const string SnapshotDirectoryName = ".slaegtsassistent";
    private const string GedcomDirectoryName = "gedcom";
    private const string ManifestFileName = "manifest.json";

    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly IAtomicFileWriter _atomicFileWriter;

    public FileSystemGedcomSnapshotStore()
        : this(new AtomicFileWriter())
    {
    }

    public FileSystemGedcomSnapshotStore(IAtomicFileWriter atomicFileWriter)
    {
        _atomicFileWriter = atomicFileWriter ?? throw new ArgumentNullException(nameof(atomicFileWriter));
    }

    public GedcomSnapshot? Load(string? outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return null;
        }

        var directory = GetSnapshotDirectory(outputDirectory);
        var manifestPath = Path.Combine(directory, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var manifest = JsonSerializer.Deserialize(
                File.ReadAllText(manifestPath),
                AppJsonContext.Default.GedcomSnapshotManifest);
            if (manifest is null)
            {
                throw new GedcomSnapshotException("GEDCOM-snapshotets manifest er tomt.");
            }

            if (manifest.FormatVersion != CurrentFormatVersion)
            {
                throw new GedcomSnapshotException(
                    $"GEDCOM-snapshotets formatversion {manifest.FormatVersion} understøttes ikke.");
            }

            var sourceCopyPath = Path.Combine(directory, manifest.SourceCopyFileName);
            if (!File.Exists(sourceCopyPath))
            {
                throw new GedcomSnapshotException(
                    "GEDCOM-snapshotets lokale fil mangler.");
            }

            var sourceHash = ComputeHash(File.ReadAllBytes(sourceCopyPath));
            if (!string.Equals(sourceHash, manifest.SourceHash, StringComparison.Ordinal))
            {
                throw new GedcomSnapshotException(
                    "GEDCOM-snapshotets integritetskontrol fejlede.");
            }

            return new GedcomSnapshot(
                manifest.FormatVersion,
                manifest.SourcePath,
                manifest.SourceFileName,
                manifest.SourceHash,
                manifest.ImportedAt,
                manifest.RawPersonSegments);
        }
        catch (GedcomSnapshotException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new GedcomSnapshotException(
                "GEDCOM-snapshotets manifest kunne ikke læses.",
                exception);
        }
        catch (IOException exception)
        {
            throw new GedcomSnapshotException(
                "GEDCOM-snapshotet kunne ikke læses.",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new GedcomSnapshotException(
                "Der mangler adgang til GEDCOM-snapshotet.",
                exception);
        }
    }

    public void Save(string outputDirectory, string sourcePath, FamilyTree familyTree)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(familyTree);

        if (!File.Exists(sourcePath))
        {
            throw new GedcomSnapshotException(
                "Den valgte GEDCOM-fil findes ikke længere.");
        }

        var directory = GetSnapshotDirectory(outputDirectory);
        Directory.CreateDirectory(directory);

        var sourceBytes = File.ReadAllBytes(sourcePath);
        var sourceHash = ComputeHash(sourceBytes);
        var sourceCopyFileName = $"source-{sourceHash}.gedcom";
        var sourceCopyPath = Path.Combine(directory, sourceCopyFileName);
        var manifestPath = Path.Combine(directory, ManifestFileName);

        var manifest = new GedcomSnapshotManifest(
            CurrentFormatVersion,
            Path.GetFullPath(sourcePath),
            Path.GetFileName(sourcePath),
            sourceHash,
            DateTimeOffset.UtcNow,
            sourceCopyFileName,
            familyTree.People
                .Where(person => !string.IsNullOrWhiteSpace(person.RecordId))
                .OrderBy(person => person.RecordId, StringComparer.Ordinal)
                .ToDictionary(
                    person => person.RecordId,
                    person => person.RawGedcom ?? string.Empty,
                    StringComparer.Ordinal));

        try
        {
            _atomicFileWriter.WriteBytes(sourceCopyPath, sourceBytes);
            _atomicFileWriter.WriteText(
                manifestPath,
                JsonSerializer.Serialize(manifest, AppJsonContext.Default.GedcomSnapshotManifest),
                Utf8WithoutBom);

            DeleteObsoleteSourceCopies(directory, sourceCopyFileName);
        }
        catch (AtomicFileWriteException exception)
        {
            throw new GedcomSnapshotException(
                $"GEDCOM-snapshotet kunne ikke gemmes sikkert. {exception.Message}",
                exception);
        }
        catch (IOException exception)
        {
            throw new GedcomSnapshotException(
                "GEDCOM-snapshotet kunne ikke gemmes.",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new GedcomSnapshotException(
                "Der mangler adgang til at gemme GEDCOM-snapshotet.",
                exception);
        }
    }

    private static string GetSnapshotDirectory(string outputDirectory)
    {
        return Path.Combine(
            Path.GetFullPath(outputDirectory),
            SnapshotDirectoryName,
            GedcomDirectoryName);
    }

    private static string ComputeHash(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content));
    }

    private static void DeleteObsoleteSourceCopies(
        string directory,
        string currentSourceCopyFileName)
    {
        try
        {
            foreach (var filePath in Directory.EnumerateFiles(directory, "source-*.gedcom"))
            {
                if (!string.Equals(
                        Path.GetFileName(filePath),
                        currentSourceCopyFileName,
                        StringComparison.Ordinal))
                {
                    TryDelete(filePath);
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

}
