using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SlaegtsAssistent.App.Services;

public sealed class GitLocator(ILocalProcessRunner? runner = null) : IGitLocator
{
    private static readonly string RuntimeHooksPath = EnsureEmptyHooksDirectory();
    private readonly ILocalProcessRunner _runner = runner ?? new LocalProcessRunner();
    private readonly object _sync = new();
    private string? _cachedPath;
    private bool _hasPositiveCache;

    public string? FindGitExecutable()
    {
        lock (_sync)
        {
            if (_hasPositiveCache)
            {
                return _cachedPath;
            }
        }

        foreach (var candidate in EnumerateCandidates())
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            if (!TryVerify(candidate))
            {
                continue;
            }

            lock (_sync)
            {
                _cachedPath = candidate;
                _hasPositiveCache = true;
                return _cachedPath;
            }
        }

        return null;
    }

    private bool TryVerify(string executable)
    {
        try
        {
            var workingDirectory = Path.GetPathRoot(executable) ?? "/";
            var result = _runner.RunAsync(
                new LocalProcessRequest(
                    executable,
                    ["-c", $"core.hooksPath={RuntimeHooksPath}", "--version"],
                    workingDirectory,
                    TimeSpan.FromSeconds(5),
                    Utf8Output: true),
                CancellationToken.None).GetAwaiter().GetResult();
            return result.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        foreach (var fromTools in EnumerateBundledCandidates())
        {
            yield return fromTools;
        }

        foreach (var known in EnumerateKnownSystemPaths())
        {
            yield return known;
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            yield break;
        }

        var fileName = OperatingSystem.IsWindows() ? "git.exe" : "git";
        foreach (var segment in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string absolute;
            try
            {
                absolute = Path.GetFullPath(Path.Combine(segment, fileName));
            }
            catch (Exception)
            {
                continue;
            }

            yield return absolute;
        }
    }

    private static IEnumerable<string> EnumerateBundledCandidates()
    {
        var rid = OperatingSystem.IsWindows()
            ? "win-x64"
            : OperatingSystem.IsMacOS()
                ? "osx-arm64"
                : "linux-x64";
        var root = Path.Combine(AppContext.BaseDirectory, "tools", "git", rid);
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(root, "cmd", "git.exe");
            yield return Path.Combine(root, "bin", "git.exe");
            yield break;
        }

        yield return Path.Combine(root, "git");
        yield return Path.Combine(root, "bin", "git");
    }

    private static IEnumerable<string> EnumerateKnownSystemPaths()
    {
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(programFiles, "Git", "cmd", "git.exe");
            yield return Path.Combine(programFilesX86, "Git", "cmd", "git.exe");
            yield return Path.Combine(localAppData, "Programs", "Git", "cmd", "git.exe");
            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            yield return "/opt/homebrew/bin/git";
            yield return "/usr/local/bin/git";
            yield return "/usr/bin/git";
            yield break;
        }

        yield return "/usr/bin/git";
        yield return "/usr/local/bin/git";
    }

    private static string EnsureEmptyHooksDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "slaegtsassistent-git");
        Directory.CreateDirectory(root);
        var hooks = Path.Combine(root, "hooks");
        Directory.CreateDirectory(hooks);
        return hooks;
    }
}
