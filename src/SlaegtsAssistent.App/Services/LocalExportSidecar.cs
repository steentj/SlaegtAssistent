using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface ILocalExportSidecar
{
    Task<byte[]> RenderGraphPdfAsync(string html, string paper, string orientation, CancellationToken cancellationToken);
}

public sealed class LocalExportSidecar(ILocalProcessRunner runner, string executable, string? script, string? libraryDirectory = null, string? fontConfigFile = null) : ILocalExportSidecar
{
    private const int MaximumOutput = 100 * 1024 * 1024;
    private const int MaximumInput = 20 * 1024 * 1024;
    private readonly SemaphoreSlim _versionLock = new(1, 1);
    private bool _versionVerified;

    internal static LocalExportSidecar? TryCreateDefault(ILocalProcessRunner runner)
    {
        string? developmentExecutable = null;
#if DEBUG
        developmentExecutable = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_EXPORT");
#endif
        var tool = LocalExportToolLocator.Find(AppContext.BaseDirectory, developmentExecutable);
        return tool is null ? null : new LocalExportSidecar(runner, tool.Executable, null, tool.LibraryDirectory, tool.FontConfigFile);
    }

    public async Task<byte[]> RenderGraphPdfAsync(string html, string paper, string orientation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(html);
        if (Encoding.UTF8.GetByteCount(html) > MaximumInput || paper is not ("A4" or "A3") || orientation is not ("portrait" or "landscape"))
            throw new ArgumentException("Grafen eller papirindstillingen er ugyldig.");
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable) ||
            (script is not null && (!Path.IsPathFullyQualified(script) || !File.Exists(script))) ||
            (libraryDirectory is not null && (!Path.IsPathFullyQualified(libraryDirectory) || !Directory.Exists(libraryDirectory))) ||
            (fontConfigFile is not null && (!Path.IsPathFullyQualified(fontConfigFile) || !File.Exists(fontConfigFile))))
            throw new InvalidOperationException("Det lokale PDF-værktøj mangler i programpakken.");

        cancellationToken.ThrowIfCancellationRequested();
        var jobId = Guid.NewGuid().ToString("N");
        using (var job = new ExportJobDirectory(jobId))
        {
            var directory = job.Path;
            var input = Path.Combine(directory, "input.html");
            var output = Path.Combine(directory, "result.pdf");
            var assets = Path.Combine(directory, "assets");
            var requestPath = Path.Combine(directory, "request.json");
            Directory.CreateDirectory(assets);
            await File.WriteAllTextAsync(input, html, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            var request = new ExportRequest(1, jobId, "graph-pdf", input, output, assets, new ExportOptions(paper, orientation));
            await File.WriteAllTextAsync(requestPath, JsonSerializer.Serialize(request, ExportJsonContext.Default.ExportRequest), cancellationToken).ConfigureAwait(false);
            LocalProcessResult result;
            var arguments = script is null ? new[] { "--request", requestPath } : new[] { script, "--request", requestPath };
            var environment = new Dictionary<string, string?>();
            if (libraryDirectory is not null && OperatingSystem.IsMacOS()) environment["DYLD_FALLBACK_LIBRARY_PATH"] = libraryDirectory;
            if (fontConfigFile is not null) environment["FONTCONFIG_FILE"] = fontConfigFile;
            await EnsureVersionAsync(directory, environment, cancellationToken).ConfigureAwait(false);
            try { result = await runner.RunAsync(new(executable, arguments, directory, TimeSpan.FromSeconds(120), environment), cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("PDF-eksporten overskred tidsgrænsen.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            ExportResult? response;
            try { response = ParseResponse(result.StandardOutput); }
            catch (JsonException) { throw new InvalidDataException("PDF-værktøjet gav et ugyldigt svar."); }
            if (response is null || response.ProtocolVersion != 1 || response.JobId != jobId || response.Warnings is null)
                throw new InvalidDataException("PDF-værktøjet gav et ugyldigt svar.");
            if (result.ExitCode != 0 && response.Status == "error" && response.OutputPath is null)
                throw new InvalidOperationException(response.ErrorCode switch
                {
                    "missing-tool" => "Det lokale PDF-værktøj eller et af dets biblioteker mangler.",
                    "invalid-input" => "Grafens eksportdata er ugyldige.",
                    "unsupported-version" => "PDF-værktøjets protokolversion understøttes ikke.",
                    "resource-blocked" => "Grafen indeholder en ressource, som ikke må bruges ved eksport.",
                    "timeout" => "PDF-eksporten overskred tidsgrænsen.",
                    _ => "PDF-værktøjet kunne ikke danne grafen."
                });
            if (result.ExitCode != 0 || response.Status != "ok" || response.OutputPath != output || response.ErrorCode is not null)
                throw new InvalidDataException("PDF-værktøjet kunne ikke danne grafen.");
            var file = new FileInfo(output);
            if (!file.Exists || file.LinkTarget is not null || file.Length < 5 || file.Length > MaximumOutput)
                throw new InvalidDataException("PDF-værktøjet gav en ugyldig eller for stor fil.");
            var bytes = await File.ReadAllBytesAsync(output, cancellationToken).ConfigureAwait(false);
            if (bytes.Length > MaximumOutput || !bytes.AsSpan().StartsWith("%PDF-"u8))
                throw new InvalidDataException("PDF-værktøjet gav en ugyldig PDF-fil.");
            return bytes;
        }
    }

    private async Task EnsureVersionAsync(string directory, IReadOnlyDictionary<string, string?> environment, CancellationToken cancellationToken)
    {
        await _versionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_versionVerified) return;
            var arguments = script is null ? new[] { "--version" } : new[] { script, "--version" };
            LocalProcessResult result;
            try { result = await runner.RunAsync(new(executable, arguments, directory, TimeSpan.FromSeconds(30), environment), cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Kontrol af PDF-værktøjet overskred tidsgrænsen.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var json = JsonDocument.Parse(result.StandardOutput);
                var fields = new HashSet<string>(StringComparer.Ordinal);
                if (json.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException();
                foreach (var field in json.RootElement.EnumerateObject())
                    if (!fields.Add(field.Name) || field.Name is not ("protocolVersion" or "toolVersion" or "rendererVersion")) throw new JsonException();
                if (result.ExitCode != 0 || fields.Count != 3 || json.RootElement.GetProperty("protocolVersion").GetInt32() != 1 ||
                    json.RootElement.GetProperty("toolVersion").GetString() != "0.1.0" ||
                    string.IsNullOrWhiteSpace(json.RootElement.GetProperty("rendererVersion").GetString())) throw new JsonException();
                _versionVerified = true;
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
            {
                throw new InvalidDataException("Det lokale PDF-værktøj mangler eller har en version, som ikke understøttes.", exception);
            }
        }
        finally { _versionLock.Release(); }
    }

    private static ExportResult? ParseResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException();
        var properties = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!properties.Add(property.Name) || property.Name is not ("protocolVersion" or "jobId" or "status" or "outputPath" or "warnings" or "errorCode"))
                throw new JsonException();
        }
        if (!properties.Contains("protocolVersion") || !properties.Contains("jobId") || !properties.Contains("status") || !properties.Contains("warnings")) throw new JsonException();
        return JsonSerializer.Deserialize(json, ExportJsonContext.Default.ExportResult);
    }
}

internal sealed record ExportOptions([property: JsonPropertyName("paper")] string Paper, [property: JsonPropertyName("orientation")] string Orientation);
internal sealed record ExportRequest(
    [property: JsonPropertyName("protocolVersion")] int ProtocolVersion,
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("inputPath")] string InputPath,
    [property: JsonPropertyName("outputPath")] string OutputPath,
    [property: JsonPropertyName("assetRoot")] string AssetRoot,
    [property: JsonPropertyName("options")] ExportOptions Options);
internal sealed record ExportResult(
    [property: JsonPropertyName("protocolVersion")] int ProtocolVersion,
    [property: JsonPropertyName("jobId")] string? JobId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("outputPath")] string? OutputPath,
    [property: JsonPropertyName("warnings")] string[]? Warnings,
    [property: JsonPropertyName("errorCode")] string? ErrorCode);

[JsonSerializable(typeof(ExportRequest))]
[JsonSerializable(typeof(ExportResult))]
internal partial class ExportJsonContext : JsonSerializerContext;
