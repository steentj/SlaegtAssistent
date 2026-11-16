using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.App.Services;

public sealed class GitVersionControlService(
    IGitLocator gitLocator,
    ILocalProcessRunner localProcessRunner) : IVersionControlService
{
    private const int MaxDiffCharacters = 8_000_000;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SlowTimeout = TimeSpan.FromSeconds(60);
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly string RuntimeRoot = EnsureRuntimeRoot();
    private static readonly string EmptyHooksPath = EnsureEmptyHooksDirectory();
    private static readonly string EmptyAttributesPath = EnsureEmptyAttributesFile();
    private static readonly HashSet<string> DangerousAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text",
        "eol",
        "crlf",
        "filter",
        "working-tree-encoding",
        "ident",
    };

    private readonly IGitLocator _gitLocator = gitLocator;
    private readonly ILocalProcessRunner _localProcessRunner = localProcessRunner;

    public async Task<VersionStatus> GetStatusAsync(string workspace, CancellationToken ct)
    {
        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await GetStatusInternalAsync(workspace, ct).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<VersionOperationResult> EnableAsync(string workspace, CancellationToken ct)
    {
        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var git = _gitLocator.FindGitExecutable();
            if (git is null)
            {
                return Fail("Git er ikke installeret, så versionsstyring er ikke tilgængelig.");
            }

            var state = await GetRepositoryStateAsync(git, workspace, ct).ConfigureAwait(false);
            if (state == RepositoryState.ForkertArkiv)
            {
                return Fail("Mappen ligger inde i et andet Git-arkiv og kan ikke versioneres her.");
            }

            if (state != RepositoryState.IngenArkiv)
            {
                await EnsureNoStagedOutsideScopeAsync(git, workspace, ct).ConfigureAwait(false);
                await EnsureAttributesAreSafeAsync(git, workspace, GetWorkspaceMarkdownFileNames(workspace), ct).ConfigureAwait(false);
            }

            if (state == RepositoryState.IngenArkiv)
            {
                var init = await RunGitAsync(git, workspace, ["init"], ct).ConfigureAwait(false);
                if (init.ExitCode != 0)
                {
                    return Fail(ParseGitFailure(init));
                }
            }

            EnsureGitIgnore(workspace);
            await EnsureLocalIdentityAndConfigAsync(git, workspace, ct).ConfigureAwait(false);

            var docs = GetWorkspaceMarkdownFileNames(workspace);
            if (docs.Count > 0)
            {
                var add = await RunGitAsync(git, workspace, BuildAddArguments(docs), ct).ConfigureAwait(false);
                if (add.ExitCode != 0)
                {
                    return Fail(ParseGitFailure(add));
                }

                var commit = await CommitAsync(git, workspace, "Start versionsstyring", docs, ct).ConfigureAwait(false);
                if (commit.ExitCode != 0)
                {
                    return Fail(ParseGitFailure(commit));
                }
            }
            else
            {
                var indexEmpty = await IsIndexEmptyAsync(git, workspace, ct).ConfigureAwait(false);
                if (!indexEmpty)
                {
                    return Fail("Indekset indeholder staged ændringer uden for dokumentomfanget.");
                }

                var commit = await CommitAsync(git, workspace, "Start versionsstyring", [], ct, allowEmpty: true).ConfigureAwait(false);
                if (commit.ExitCode != 0)
                {
                    return Fail(ParseGitFailure(commit));
                }
            }

            return new VersionOperationResult(true, null);
        }
        catch (VersionControlException exception)
        {
            return Fail(exception.Message);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<VersionHistory> GetHistoryAsync(string workspace, int maxEntries, CancellationToken ct)
    {
        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var git = RequireGit();
            var state = await GetRepositoryStateAsync(git, workspace, ct).ConfigureAwait(false);
            if (state != RepositoryState.Klar)
            {
                return new VersionHistory([], false);
            }

            var result = await RunGitAsync(git, workspace, ["log", "-n", (maxEntries + 1).ToString(CultureInfo.InvariantCulture), "--no-color", "--format=%H%x1f%cI%x1f%s%x1e"], ct).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                return new VersionHistory([], false);
            }

            return GitLogParser.Parse(result.StandardOutput, maxEntries);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<VersionOperationResult> CommitAllAsync(string workspace, string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Fail("Skriv en commit-besked, før du fortsætter.");
        }

        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var git = RequireGit();
            return await CommitAllCoreAsync(git, workspace, message, ct).ConfigureAwait(false);
        }
        catch (VersionControlException exception)
        {
            return Fail(exception.Message);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<IReadOnlyList<DiffFile>> GetWorkingChangesAsync(string workspace, CancellationToken ct)
    {
        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var git = RequireGit();
            var state = await GetRepositoryStateAsync(git, workspace, ct).ConfigureAwait(false);
            if (state != RepositoryState.Klar)
            {
                return [];
            }

            var existingDocs = GetWorkspaceMarkdownFileNames(workspace);
            if (existingDocs.Count > 0)
            {
                var addIntent = await RunGitAsync(git, workspace, BuildAddIntentArguments(existingDocs), ct).ConfigureAwait(false);
                if (addIntent.ExitCode != 0)
                {
                    throw new VersionControlException(ParseGitFailure(addIntent));
                }
            }

            var diff = await RunGitAsync(
                git,
                workspace,
                ["diff", "--no-color", "--no-ext-diff", "--no-textconv", "--find-renames", "HEAD"],
                ct,
                SlowTimeout,
                maxOutputCharacters: MaxDiffCharacters).ConfigureAwait(false);
            if (diff.ExitCode != 0)
            {
                throw new VersionControlException(ParseGitFailure(diff));
            }

            return UnifiedDiffParser.Parse(diff.StandardOutput)
                .Where(file => IsRootMarkdownPath(file.Path))
                .ToArray();
        }
        catch (InvalidDataException)
        {
            throw new VersionControlException("Diffen er for stor til at blive vist.");
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<IReadOnlyList<DiffFile>> GetCommitChangesAsync(string workspace, string commitId, CancellationToken ct)
    {
        if (!CommitIdValidator.IsValid(commitId))
        {
            throw new VersionControlException("Versions-id'et er ugyldigt.");
        }

        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var git = RequireGit();
            var verify = await RunGitAsync(git, workspace, ["cat-file", "-e", $"{commitId}^{{commit}}"], ct).ConfigureAwait(false);
            if (verify.ExitCode != 0)
            {
                throw new VersionControlException("Versionen findes ikke længere.");
            }

            var diff = await RunGitAsync(
                git,
                workspace,
                ["show", "--no-color", "--no-ext-diff", "--no-textconv", "--format=", "--find-renames", commitId],
                ct,
                SlowTimeout,
                maxOutputCharacters: MaxDiffCharacters).ConfigureAwait(false);
            if (diff.ExitCode != 0)
            {
                throw new VersionControlException(ParseGitFailure(diff));
            }

            return UnifiedDiffParser.Parse(diff.StandardOutput)
                .Where(file => IsRootMarkdownPath(file.Path))
                .ToArray();
        }
        catch (InvalidDataException)
        {
            throw new VersionControlException("Diffen er for stor til at blive vist.");
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<VersionOperationResult> RollbackToAsync(string workspace, string commitId, CancellationToken ct)
    {
        if (!CommitIdValidator.IsValid(commitId))
        {
            return Fail("Versions-id'et er ugyldigt.");
        }

        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var git = RequireGit();
            var verify = await RunGitAsync(git, workspace, ["cat-file", "-e", $"{commitId}^{{commit}}"], ct).ConfigureAwait(false);
            if (verify.ExitCode != 0)
            {
                return Fail("Versionen findes ikke længere.");
            }

            await EnsureNoStagedOutsideScopeAsync(git, workspace, ct).ConfigureAwait(false);
            await EnsureAttributesAreSafeAsync(git, workspace, GetWorkspaceMarkdownFileNames(workspace), ct).ConfigureAwait(false);

            var current = SnapshotWorkspaceMarkdownFiles(workspace);
            var target = await LoadCommitSnapshotAsync(git, workspace, commitId, ct).ConfigureAwait(false);
            if (SnapshotsEqual(current, target))
            {
                return new VersionOperationResult(true, "Dokumenterne er allerede i denne tilstand.");
            }

            var targetInfo = await RunGitAsync(git, workspace, ["log", "-1", "--format=%cI%x1f%s", commitId], ct).ConfigureAwait(false);
            if (targetInfo.ExitCode != 0)
            {
                return Fail(ParseGitFailure(targetInfo));
            }

            var fields = targetInfo.StandardOutput.Trim().Split('\x1f');
            var targetTime = fields.Length > 0 ? DateTimeOffset.Parse(fields[0], CultureInfo.InvariantCulture).ToLocalTime() : DateTimeOffset.Now;
            var targetMessage = fields.Length > 1 ? fields[1] : string.Empty;
            var rollbackMessage = $"Rul tilbage til {targetTime:dd-MM-yyyy HH:mm}: {targetMessage}";

            var status = await GetStatusInternalAsync(workspace, ct).ConfigureAwait(false);
            if (status.ChangedFileCount > 0)
            {
                var autoCommit = await CommitAllCoreAsync(git, workspace, "Automatisk version før tilbagerulning", ct).ConfigureAwait(false);
                if (!autoCommit.Success)
                {
                    return autoCommit;
                }
            }

            var headBefore = await ResolveHeadAsync(git, workspace, ct).ConfigureAwait(false);
            var restorePaths = target.Keys.OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (restorePaths.Length > 0)
            {
                var restore = await RunGitAsync(git, workspace, BuildRestoreArguments(commitId, restorePaths), ct, SlowTimeout).ConfigureAwait(false);
                if (restore.ExitCode != 0)
                {
                    return Fail(ParseGitFailure(restore));
                }
            }

            foreach (var file in Directory.GetFiles(workspace, "*.md", SearchOption.TopDirectoryOnly))
            {
                if (target.ContainsKey(Path.GetFileName(file)))
                {
                    continue;
                }

                File.Delete(file);
            }

            await EnsureAttributesAreSafeAsync(git, workspace, GetWorkspaceMarkdownFileNames(workspace), ct).ConfigureAwait(false);
            await EnsureNoStagedOutsideScopeAsync(git, workspace, ct).ConfigureAwait(false);

            var documentPaths = await GetDocumentScopePathsAsync(git, workspace, ct).ConfigureAwait(false);
            var commit = await CommitAsync(git, workspace, rollbackMessage, documentPaths, ct).ConfigureAwait(false);
            if (commit.ExitCode != 0)
            {
                await RestoreToCommitSnapshotAsync(git, workspace, headBefore, ct).ConfigureAwait(false);
                return Fail(ParseGitFailure(commit));
            }

            return new VersionOperationResult(true, rollbackMessage);
        }
        catch (VersionControlException exception)
        {
            return Fail(exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Fail($"Tilbagerulning mislykkedes: {exception.Message}");
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<VersionStatus> GetStatusInternalAsync(string workspace, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
        {
            return new VersionStatus(
                VersionControlState.IkkeAktiveret,
                "Vælg først en gyldig arbejdsmappe i Indstillinger.",
                [],
                null);
        }

        var git = _gitLocator.FindGitExecutable();
        if (git is null)
        {
            return new VersionStatus(
                VersionControlState.GitMangler,
                "Git er ikke installeret, så versionsstyring er ikke tilgængelig.",
                [],
                null);
        }

        var state = await GetRepositoryStateAsync(git, workspace, ct).ConfigureAwait(false);
        if (state == RepositoryState.ForkertArkiv)
        {
            return new VersionStatus(
                VersionControlState.ForkertArkiv,
                "Mappen ligger inde i et andet Git-arkiv og kan ikke versioneres her.",
                [],
                null);
        }

        if (state != RepositoryState.Klar)
        {
            return new VersionStatus(
                VersionControlState.IkkeAktiveret,
                "Versionsstyring er ikke slået til for denne mappe.",
                [],
                null);
        }

        var status = await RunGitAsync(
            git,
            workspace,
            ["status", "--porcelain=v1", "-z", "--untracked-files=all"],
            ct).ConfigureAwait(false);
        if (status.ExitCode != 0)
        {
            return new VersionStatus(VersionControlState.Klar, ParseGitFailure(status), [], null);
        }

        var changes = GitStatusParser.Parse(status.StandardOutput)
            .Where(change => IsRootMarkdownPath(change.Path))
            .ToArray();
        DateTimeOffset? lastChange = null;
        foreach (var change in changes.Where(change => change.Kind != WorkingTreeChangeKind.Slettet))
        {
            var path = Path.Combine(workspace, change.Path);
            if (!File.Exists(path))
            {
                continue;
            }

            var modified = new DateTimeOffset(File.GetLastWriteTime(path));
            if (!lastChange.HasValue || modified > lastChange.Value)
            {
                lastChange = modified;
            }
        }

        return new VersionStatus(VersionControlState.Klar, null, changes, lastChange);
    }

    private async Task<RepositoryState> GetRepositoryStateAsync(string git, string workspace, CancellationToken ct)
    {
        var root = await RunGitAsync(git, workspace, ["rev-parse", "--show-prefix"], ct).ConfigureAwait(false);
        if (root.ExitCode != 0)
        {
            return RepositoryState.IngenArkiv;
        }

        if (!string.IsNullOrWhiteSpace(root.StandardOutput.Trim()))
        {
            return RepositoryState.ForkertArkiv;
        }

        var hasHead = await RunGitAsync(git, workspace, ["rev-parse", "--verify", "--quiet", "HEAD"], ct).ConfigureAwait(false);
        return hasHead.ExitCode == 0 ? RepositoryState.Klar : RepositoryState.IngenHead;
    }

    private async Task EnsureNoStagedOutsideScopeAsync(string git, string workspace, CancellationToken ct)
    {
        var staged = await RunGitAsync(git, workspace, ["diff", "--cached", "--name-only", "-z"], ct).ConfigureAwait(false);
        if (staged.ExitCode != 0)
        {
            throw new VersionControlException(ParseGitFailure(staged));
        }

        foreach (var path in staged.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsRootMarkdownPath(path))
            {
                continue;
            }

            throw new VersionControlException(
                "Der er staged ændringer uden for dokumentomfanget. Fjern dem fra indekset, før du fortsætter.");
        }
    }

    private async Task EnsureAttributesAreSafeAsync(string git, string workspace, IReadOnlyList<string> markdownFileNames, CancellationToken ct)
    {
        if (markdownFileNames.Count == 0)
        {
            return;
        }

        var args = new List<string> { "check-attr", "-z", "--all", "--" };
        args.AddRange(markdownFileNames);
        var check = await RunGitAsync(git, workspace, args, ct).ConfigureAwait(false);
        if (check.ExitCode != 0)
        {
            throw new VersionControlException("Kunne ikke kontrollere Git-attributter for dokumenterne.");
        }

        var parts = check.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i + 2 < parts.Length; i += 3)
        {
            var attribute = parts[i + 1];
            var value = parts[i + 2];
            if (!DangerousAttributes.Contains(attribute))
            {
                continue;
            }

            if (string.Equals(value, "unspecified", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "unset", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            throw new VersionControlException(
                "Aktive Git-attributter kan ændre dokumenternes bytes. Fjern attributterne før versionsstyring.");
        }
    }

    private async Task<LocalProcessResult> CommitAsync(
        string git,
        string workspace,
        string message,
        IReadOnlyList<string> documentPaths,
        CancellationToken ct,
        bool allowEmpty = false)
    {
        var args = await BuildCommitArgumentsAsync(git, workspace, message, documentPaths, allowEmpty, ct).ConfigureAwait(false);
        return await RunGitAsync(git, workspace, args, ct).ConfigureAwait(false);
    }

    private async Task<VersionOperationResult> CommitAllCoreAsync(
        string git,
        string workspace,
        string message,
        CancellationToken ct)
    {
        var status = await GetStatusInternalAsync(workspace, ct).ConfigureAwait(false);
        if (status.State != VersionControlState.Klar)
        {
            return Fail(status.Message ?? "Versionsstyring er ikke klar.");
        }

        if (status.ChangedFileCount == 0)
        {
            return Fail("Der er ingen ændringer at committe.");
        }

        await EnsureNoStagedOutsideScopeAsync(git, workspace, ct).ConfigureAwait(false);
        await EnsureAttributesAreSafeAsync(git, workspace, GetWorkspaceMarkdownFileNames(workspace), ct).ConfigureAwait(false);

        var documentPaths = await GetDocumentScopePathsAsync(git, workspace, ct).ConfigureAwait(false);
        var add = await RunGitAsync(git, workspace, BuildAddArguments(documentPaths), ct).ConfigureAwait(false);
        if (add.ExitCode != 0)
        {
            return Fail(ParseGitFailure(add));
        }

        var commit = await CommitAsync(git, workspace, message.Trim(), documentPaths, ct).ConfigureAwait(false);
        if (commit.ExitCode != 0)
        {
            if (commit.StandardError.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
            {
                return Fail("Der er ingen ændringer at committe.");
            }

            return Fail(ParseGitFailure(commit));
        }

        return new VersionOperationResult(true, null);
    }

    private async Task<IReadOnlyList<string>> BuildCommitArgumentsAsync(
        string git,
        string workspace,
        string message,
        IReadOnlyList<string> documentPaths,
        bool allowEmpty,
        CancellationToken ct)
    {
        var arguments = new List<string>();
        var userName = await RunGitAsync(git, workspace, ["config", "--get", "user.name"], ct).ConfigureAwait(false);
        var userEmail = await RunGitAsync(git, workspace, ["config", "--get", "user.email"], ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(userName.StandardOutput))
        {
            arguments.Add("-c");
            arguments.Add("user.name=Slægtsassistent");
        }

        if (string.IsNullOrWhiteSpace(userEmail.StandardOutput))
        {
            arguments.Add("-c");
            arguments.Add("user.email=slaegtsassistent@localhost");
        }

        arguments.Add("commit");
        if (documentPaths.Count > 0)
        {
            arguments.Add("--only");
        }
        if (allowEmpty)
        {
            arguments.Add("--allow-empty");
        }
        arguments.Add("-m");
        arguments.Add(message);
        if (documentPaths.Count > 0)
        {
            arguments.Add("--");
            foreach (var path in documentPaths)
            {
                arguments.Add(path);
            }
        }
        return arguments;
    }

    private async Task EnsureLocalIdentityAndConfigAsync(string git, string workspace, CancellationToken ct)
    {
        var userName = await RunGitAsync(git, workspace, ["config", "--get", "user.name"], ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(userName.StandardOutput))
        {
            var setName = await RunGitAsync(git, workspace, ["config", "user.name", "Slægtsassistent"], ct).ConfigureAwait(false);
            if (setName.ExitCode != 0)
            {
                throw new VersionControlException(ParseGitFailure(setName));
            }
        }

        var userEmail = await RunGitAsync(git, workspace, ["config", "--get", "user.email"], ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(userEmail.StandardOutput))
        {
            var setEmail = await RunGitAsync(git, workspace, ["config", "user.email", "slaegtsassistent@localhost"], ct).ConfigureAwait(false);
            if (setEmail.ExitCode != 0)
            {
                throw new VersionControlException(ParseGitFailure(setEmail));
            }
        }

        var setAutoCrlf = await RunGitAsync(git, workspace, ["config", "core.autocrlf", "false"], ct).ConfigureAwait(false);
        if (setAutoCrlf.ExitCode != 0)
        {
            throw new VersionControlException(ParseGitFailure(setAutoCrlf));
        }
    }

    private static void EnsureGitIgnore(string workspace)
    {
        var path = Path.Combine(workspace, ".gitignore");
        if (File.Exists(path))
        {
            return;
        }

        var content = """
                      # Slægtsassistent versionerer kun persondokumenter (Markdown) i denne mappe.
                      /*
                      !/.gitignore
                      !/*.md
                      """;
        var temp = Path.Combine(workspace, ".gitignore.tmp");
        File.WriteAllText(temp, content.Replace("\r\n", "\n"), new System.Text.UTF8Encoding(false));
        File.Move(temp, path, overwrite: true);
    }

    private static bool IsRootMarkdownPath(string path)
    {
        if (path.Contains('/') || path.Contains('\\'))
        {
            return false;
        }

        return path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetWorkspaceMarkdownFileNames(string workspace)
        => Directory.GetFiles(workspace, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();

    private async Task<IReadOnlyList<string>> GetDocumentScopePathsAsync(string git, string workspace, CancellationToken ct)
    {
        var existing = GetWorkspaceMarkdownFileNames(workspace);
        var trackedResult = await RunGitAsync(git, workspace, ["ls-files", "-z"], ct).ConfigureAwait(false);
        if (trackedResult.ExitCode != 0)
        {
            throw new VersionControlException(ParseGitFailure(trackedResult));
        }

        var scope = new HashSet<string>(existing, StringComparer.Ordinal);
        foreach (var path in trackedResult.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsRootMarkdownPath(path))
            {
                scope.Add(path);
            }
        }

        return scope.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    private static Dictionary<string, byte[]> SnapshotWorkspaceMarkdownFiles(string workspace)
    {
        var snapshot = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(workspace, "*.md", SearchOption.TopDirectoryOnly))
        {
            snapshot[Path.GetFileName(file)] = File.ReadAllBytes(file);
        }

        return snapshot;
    }

    private async Task<Dictionary<string, byte[]>> LoadCommitSnapshotAsync(string git, string workspace, string commitId, CancellationToken ct)
    {
        var filesResult = await RunGitAsync(git, workspace, ["ls-tree", "-r", "--name-only", commitId], ct).ConfigureAwait(false);
        if (filesResult.ExitCode != 0)
        {
            throw new VersionControlException(ParseGitFailure(filesResult));
        }

        var files = filesResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var snapshot = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (!IsRootMarkdownPath(file))
            {
                continue;
            }

            var show = await RunGitAsync(git, workspace, ["show", $"{commitId}:{file}"], ct, SlowTimeout, maxOutputCharacters: MaxDiffCharacters).ConfigureAwait(false);
            if (show.ExitCode != 0)
            {
                throw new VersionControlException(ParseGitFailure(show));
            }

            snapshot[file] = System.Text.Encoding.UTF8.GetBytes(show.StandardOutput);
        }

        return snapshot;
    }

    private static bool SnapshotsEqual(IReadOnlyDictionary<string, byte[]> current, IReadOnlyDictionary<string, byte[]> target)
    {
        if (current.Count != target.Count)
        {
            return false;
        }

        foreach (var item in current)
        {
            if (!target.TryGetValue(item.Key, out var targetBytes))
            {
                return false;
            }

            if (!item.Value.AsSpan().SequenceEqual(targetBytes))
            {
                return false;
            }
        }

        return true;
    }

    private async Task RestoreToCommitSnapshotAsync(string git, string workspace, string commitId, CancellationToken ct)
    {
        var target = await LoadCommitSnapshotAsync(git, workspace, commitId, ct).ConfigureAwait(false);
        var restorePaths = target.Keys.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        if (restorePaths.Length > 0)
        {
            var restore = await RunGitAsync(git, workspace, BuildRestoreArguments(commitId, restorePaths), ct, SlowTimeout).ConfigureAwait(false);
            if (restore.ExitCode != 0)
            {
                throw new VersionControlException(ParseGitFailure(restore));
            }
        }

        foreach (var file in Directory.GetFiles(workspace, "*.md", SearchOption.TopDirectoryOnly))
        {
            if (!target.ContainsKey(Path.GetFileName(file)))
            {
                File.Delete(file);
            }
        }
    }

    private static IReadOnlyList<string> BuildAddArguments(IReadOnlyList<string> documentPaths)
    {
        var args = new List<string> { "add", "-A", "--" };
        args.AddRange(documentPaths);
        return args;
    }

    private static IReadOnlyList<string> BuildAddIntentArguments(IReadOnlyList<string> documentPaths)
    {
        var args = new List<string> { "add", "-N", "--" };
        args.AddRange(documentPaths);
        return args;
    }

    private static IReadOnlyList<string> BuildRestoreArguments(string commitId, IReadOnlyList<string> documentPaths)
    {
        var args = new List<string> { "restore", $"--source={commitId}", "--staged", "--worktree", "--" };
        args.AddRange(documentPaths);
        return args;
    }

    private async Task<string> ResolveHeadAsync(string git, string workspace, CancellationToken ct)
    {
        var head = await RunGitAsync(git, workspace, ["rev-parse", "HEAD"], ct).ConfigureAwait(false);
        if (head.ExitCode != 0)
        {
            throw new VersionControlException(ParseGitFailure(head));
        }

        return head.StandardOutput.Trim();
    }

    private async Task<bool> IsIndexEmptyAsync(string git, string workspace, CancellationToken ct)
    {
        var result = await RunGitAsync(git, workspace, ["diff", "--cached", "--name-only"], ct).ConfigureAwait(false);
        return result.ExitCode == 0 && string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    private async Task<LocalProcessResult> RunGitAsync(
        string git,
        string workspace,
        IReadOnlyList<string> arguments,
        CancellationToken ct,
        TimeSpan? timeout = null,
        int maxOutputCharacters = 1_048_576)
    {
        var prefixed = new List<string>
        {
            "-C", workspace,
            "--no-pager",
            "-c", "core.quotepath=false",
            "-c", "core.autocrlf=false",
            "-c", "core.safecrlf=false",
            "-c", $"core.attributesFile={EmptyAttributesPath}",
            "-c", $"core.hooksPath={EmptyHooksPath}",
            "-c", "commit.gpgsign=false",
        };
        prefixed.AddRange(arguments);

        return await _localProcessRunner.RunAsync(
            new LocalProcessRequest(
                git,
                prefixed,
                workspace,
                timeout ?? DefaultTimeout,
                Environment: new Dictionary<string, string>
                {
                    ["GIT_TERMINAL_PROMPT"] = "0",
                    ["LC_ALL"] = "C",
                    ["GIT_OPTIONAL_LOCKS"] = "0",
                },
                Utf8Output: true,
                MaximumOutputCharacters: maxOutputCharacters),
            ct).ConfigureAwait(false);
    }

    private static VersionOperationResult Fail(string message) => new(false, message);

    private static string ParseGitFailure(LocalProcessResult result)
    {
        var stderr = (result.StandardError ?? string.Empty).Trim();
        if (stderr.Contains("index.lock", StringComparison.OrdinalIgnoreCase))
        {
            return "Et andet program bruger versionsarkivet lige nu. Prøv igen om lidt.";
        }

        var line = stderr.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(line))
        {
            return "Git-kommandoen fejlede.";
        }

        if (line.Length > 300)
        {
            line = line[..300];
        }
        return $"Git-kommandoen fejlede: {line}";
    }

    private string RequireGit()
        => _gitLocator.FindGitExecutable()
           ?? throw new VersionControlException("Git er ikke installeret, så versionsstyring er ikke tilgængelig.");

    private static string EnsureRuntimeRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "slaegtsassistent-versioning");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string EnsureEmptyHooksDirectory()
    {
        var path = Path.Combine(RuntimeRoot, "hooks");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string EnsureEmptyAttributesFile()
    {
        var path = Path.Combine(RuntimeRoot, "attributes");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, string.Empty, new System.Text.UTF8Encoding(false));
        }

        return path;
    }

    private enum RepositoryState
    {
        IngenArkiv,
        IngenHead,
        ForkertArkiv,
        Klar,
    }
}
