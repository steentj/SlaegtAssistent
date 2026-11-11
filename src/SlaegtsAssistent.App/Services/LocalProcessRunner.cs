using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed record LocalProcessRequest(string Executable, IReadOnlyList<string> Arguments, string WorkingDirectory, TimeSpan Timeout);
public sealed record LocalProcessResult(int ExitCode, string StandardOutput, string StandardError);

public interface ILocalProcessRunner
{
    Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken);
}

public sealed class LocalProcessRunner : ILocalProcessRunner
{
    public async Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Path.IsPathFullyQualified(request.Executable) || !Path.IsPathFullyQualified(request.WorkingDirectory) || request.Timeout <= TimeSpan.Zero)
            throw new ArgumentException("Programsti, arbejdsmappe eller tidsgrænse er ugyldig.", nameof(request));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request.Timeout);
        var start = new ProcessStartInfo(request.Executable)
        {
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in request.Arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        process.Start();
        using var kill = timeout.Token.Register(() => { try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } });
        try
        {
            var stdout = ReadBoundedAsync(process.StandardOutput, 1024 * 1024, timeout.Token);
            var stderr = ReadBoundedAsync(process.StandardError, 4 * 1024 * 1024, timeout.Token);
            var exited = process.WaitForExitAsync(timeout.Token);
            var completed = await Task.WhenAny(stdout, stderr, exited).ConfigureAwait(false);
            if (completed.IsFaulted || completed.IsCanceled) await completed.ConfigureAwait(false);
            if (completed != exited)
            {
                var remaining = completed == stdout ? stderr : stdout;
                completed = await Task.WhenAny(remaining, exited).ConfigureAwait(false);
                if (completed.IsFaulted || completed.IsCanceled) await completed.ConfigureAwait(false);
            }
            await exited.ConfigureAwait(false);
            var output = await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            return new LocalProcessResult(process.ExitCode, output[0], output[1]);
        }
        catch
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, int maximum, CancellationToken token)
    {
        var result = new StringBuilder();
        var buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) != 0)
        {
            if (result.Length + read > maximum) throw new InvalidDataException("Et lokalt værktøj gav for meget output.");
            result.Append(buffer, 0, read);
        }
        return result.ToString();
    }
}
