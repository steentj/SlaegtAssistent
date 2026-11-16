using System;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed class MediaWorkspaceIndex : IDisposable
{
    private readonly object _gate = new();
    private string? _folder;
    private CancellationTokenSource? _cancellation;
    private Task<MediaFileIndex>? _task;
    private bool _disposed;

    public async Task<MediaFileIndex?> GetAsync(string? folder, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folder)) return null;
        Task<MediaFileIndex> task;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!string.Equals(folder, _folder, StringComparison.Ordinal) || _task is null || _task.IsCanceled || _task.IsFaulted)
            {
                CancelCurrent();
                _folder = folder;
                _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _task = MediaFileIndex.BuildAsync(folder,
                    OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, _cancellation.Token);
            }
            task = _task;
        }
        return await task.WaitAsync(cancellationToken);
    }

    public void Invalidate()
    {
        lock (_gate) CancelCurrent();
    }

    private void CancelCurrent()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        _task = null;
        _folder = null;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            CancelCurrent();
        }
    }
}
