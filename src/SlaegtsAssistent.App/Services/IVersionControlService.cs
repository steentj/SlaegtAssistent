using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.App.Services;

public enum VersionControlState
{
    GitMangler,
    IkkeAktiveret,
    ForkertArkiv,
    Klar,
}

public sealed record VersionStatus(
    VersionControlState State,
    string? Message,
    IReadOnlyList<WorkingTreeChange> Changes,
    DateTimeOffset? LastChangeTime)
{
    public int ChangedFileCount => Changes.Count;
}

public sealed record VersionOperationResult(bool Success, string? Message);

public sealed class VersionControlException(string message) : Exception(message);

public interface IVersionControlService
{
    Task<VersionStatus> GetStatusAsync(string workspace, CancellationToken ct);
    Task<VersionOperationResult> EnableAsync(string workspace, CancellationToken ct);
    Task<VersionHistory> GetHistoryAsync(string workspace, int maxEntries, CancellationToken ct);
    Task<VersionOperationResult> CommitAllAsync(string workspace, string message, CancellationToken ct);
    Task<IReadOnlyList<DiffFile>> GetWorkingChangesAsync(string workspace, CancellationToken ct);
    Task<IReadOnlyList<DiffFile>> GetCommitChangesAsync(string workspace, string commitId, CancellationToken ct);
    Task<VersionOperationResult> RollbackToAsync(string workspace, string commitId, CancellationToken ct);
}
