using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Services;

// Kontoreksporten bruger redigerede dokumenter; Markdown-generatoren har et separat ansvar.
public interface IBiographyConverter
{
    Task<BiographyConversionResult> RenderBiographyAsync(IReadOnlyList<BiographyExportItem> items,
        BiographyExportMode mode, BiographyExportFormat format, IReadOnlyCollection<string> allowedRoots,
        CancellationToken cancellationToken);
}

public sealed record BiographyExportTarget(IReadOnlyList<BiographyExportItem> Items, string Path);
public sealed record BiographyExportProgress(string Phase, int Current, int Total);
public sealed record BiographyExportOutcome(IReadOnlyList<string> Saved, IReadOnlyList<string> NotSaved,
    IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings, bool Cancelled);

public sealed class BiographyExportService(IBiographyConverter converter, IAtomicFileWriter writer)
{
    public static IReadOnlyList<BiographyExportTarget> PlanTargets(IReadOnlyList<BiographyExportItem> items,
        BiographyExportMode mode, BiographyExportFormat format, string destination)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        if (items.Count is 0 or > 100 || items.Select(x => x.RecordId).Distinct(StringComparer.Ordinal).Count() != items.Count)
            throw new ArgumentException("Vælg mellem 1 og 100 entydige biografier.");
        var extension = format switch { BiographyExportFormat.Docx => ".docx", BiographyExportFormat.Pdf => ".pdf", BiographyExportFormat.Odt => ".odt", _ => throw new ArgumentOutOfRangeException(nameof(format)) };
        if (mode == BiographyExportMode.Combined) return [new(items.ToArray(), Path.GetFullPath(destination))];
        if (mode != BiographyExportMode.Separate) throw new ArgumentOutOfRangeException(nameof(mode));
        var directory = Path.GetFullPath(destination);
        var names = items.Select(item => SafeName(Path.GetFileNameWithoutExtension(item.SourcePath))).ToArray();
        var hashes = items.Select(item => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(item.RecordId))).ToLowerInvariant()).ToArray();
        var resolved = names.ToArray();
        for (var round = 0; round < 2; round++)
        {
            var collisions = resolved.GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (collisions.Count == 0) break;
            for (var i = 0; i < resolved.Length; i++)
                if (collisions.Contains(resolved[i])) resolved[i] = names[i] + "-" + hashes[i][..(round == 0 ? 16 : 64)];
        }
        var targets = items.Select((item, i) => new BiographyExportTarget([item], Path.Combine(directory, resolved[i] + extension))).ToArray();
        if (targets.Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != targets.Length)
            throw new ArgumentException("Der kunne ikke dannes entydige filnavne.");
        return targets;
    }

    private static string SafeName(string value)
    {
        var invalid = "<>:\"/\\|?*";
        var name = new string(value.Select(c => c < 32 || invalid.Contains(c) ? '-' : c).ToArray()).Trim().TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(name)) name = "person";
        var stem = name.Split('.')[0].TrimEnd(' ');
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³" }.Contains(stem, StringComparer.OrdinalIgnoreCase)) name = "person-" + name;
        var result = new StringBuilder();
        var length = 0;
        foreach (var rune in name.EnumerateRunes())
        {
            if (length + rune.Utf8SequenceLength > 160) break;
            result.Append(rune.ToString());
            length += rune.Utf8SequenceLength;
        }
        return result.ToString().TrimEnd('.', ' ');
    }

    public async Task<BiographyExportOutcome> ExportAsync(IReadOnlyList<BiographyExportTarget> targets,
        BiographyExportMode mode, BiographyExportFormat format, IReadOnlyCollection<string> allowedRoots,
        IProgress<BiographyExportProgress>? progress, CancellationToken cancellationToken, IReadOnlyCollection<string>? approvedExistingPaths = null, IReadOnlyCollection<string>? approvedWarnings = null, IReadOnlyList<string>? approvedFingerprints = null)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count == 0 || targets.Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != targets.Count)
            throw new ArgumentException("Eksportens mål er ugyldige.");
        if (approvedFingerprints is not null && approvedFingerprints.Count != targets.Count)
            throw new ArgumentException("Eksportens gennemgåede dokumentgrundlag er ugyldigt.");
        var saved = new List<string>();
        var warnings = new List<string>();
        var errors = new List<string>();
        using var job = new ExportJobDirectory(Guid.NewGuid().ToString("N"));
        try
        {
            for (var i = 0; i < targets.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new("Konverterer", i + 1, targets.Count));
                var converted = await converter.RenderBiographyAsync(targets[i].Items, mode, format, allowedRoots, cancellationToken).ConfigureAwait(false);
                if (approvedFingerprints is not null && !string.Equals(converted.Fingerprint, approvedFingerprints[i], StringComparison.Ordinal))
                    throw new InvalidOperationException("Dokumentindhold eller billeder blev ændret efter gennemgangen. Gennemgå eksporten igen.");
                warnings.AddRange(converted.Warnings);
                await File.WriteAllBytesAsync(Path.Combine(job.Path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)), converted.Content, cancellationToken).ConfigureAwait(false);
            }
            if (approvedWarnings is not null && warnings.Except(approvedWarnings, StringComparer.Ordinal).Any())
                throw new InvalidOperationException("Eksportens advarsler ændrede sig under konverteringen. Gennemgå eksporten igen.");
            for (var i = 0; i < targets.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new("Gemmer", i + 1, targets.Count));
                var bytes = await File.ReadAllBytesAsync(Path.Combine(job.Path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                var approved = approvedExistingPaths?.Contains(targets[i].Path, StringComparer.OrdinalIgnoreCase) ?? false;
                if (approved) writer.WriteBytes(targets[i].Path, bytes);
                else if (File.Exists(targets[i].Path))
                    throw new IOException($"Målfilen findes nu og blev ikke godkendt til overskrivning: {targets[i].Path}");
                else if (writer is INoOverwriteAtomicFileWriter exclusive) exclusive.WriteNewBytes(targets[i].Path, bytes);
                else throw new InvalidOperationException("Filsystemet understøtter ikke sikker oprettelse uden overskrivning.");
                saved.Add(targets[i].Path);
            }
            return new(saved, [], [], warnings.Distinct(StringComparer.Ordinal).ToArray(), false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(saved, targets.Select(x => x.Path).Except(saved, StringComparer.OrdinalIgnoreCase).ToArray(), errors, warnings.Distinct(StringComparer.Ordinal).ToArray(), true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            errors.Add(exception.Message);
            return new(saved, targets.Select(x => x.Path).Except(saved, StringComparer.OrdinalIgnoreCase).ToArray(), errors, warnings.Distinct(StringComparer.Ordinal).ToArray(), false);
        }
    }
}
