using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Versioning;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;

namespace SlaegtsAssistent.App.ViewModels;

public sealed partial class VersionHistoryViewModel : ViewModelBase, IDisposable
{
    private readonly IVersioningHost _host;
    private readonly IVersionControlService _versionControlService;
    private readonly VersioningWorkflow _workflow;
    private CancellationTokenSource? _refreshCancellation;
    private bool _disposed;

    [ObservableProperty]
    private string statusText = "Opdaterer …";

    [ObservableProperty]
    private string? extraStatusText;

    [ObservableProperty]
    private bool hasChanges;

    [ObservableProperty]
    private bool showEnableButton;

    [ObservableProperty]
    private string? historyNote;

    [ObservableProperty]
    private string? infoMessage;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool canShowWorkingChanges;

    [ObservableProperty]
    private bool canCommitAll;

    public ObservableCollection<VersionEntryViewModel> Entries { get; } = [];

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ShowChangesCommand { get; }
    public IAsyncRelayCommand CommitAllCommand { get; }
    public IAsyncRelayCommand EnableCommand { get; }

    public VersionHistoryViewModel(
        IVersioningHost host,
        IVersionControlService versionControlService,
        VersioningWorkflow workflow)
    {
        _host = host;
        _versionControlService = versionControlService;
        _workflow = workflow;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ShowChangesCommand = new AsyncRelayCommand(ShowWorkingChangesAsync, CanRunAction);
        CommitAllCommand = new AsyncRelayCommand(CommitAllAsync, CanRunAction);
        EnableCommand = new AsyncRelayCommand(EnableAsync, CanEnable);
        _host.DocumentsSaved += HandleDocumentsSaved;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _host.DocumentsSaved -= HandleDocumentsSaved;
        _refreshCancellation?.Cancel();
        _refreshCancellation?.Dispose();
        _refreshCancellation = null;
    }

    private bool CanRunAction()
        => !IsBusy && !_host.IsBusyWithImport && HasChanges && !ShowEnableButton;

    private bool CanEnable()
        => !IsBusy && !_host.IsBusyWithImport && ShowEnableButton;

    private async Task EnableAsync()
    {
        var workspace = _host.WorkspaceFolder;
        if (string.IsNullOrWhiteSpace(workspace))
        {
            ErrorMessage = "Vælg først en arbejdsmappe i Indstillinger.";
            return;
        }

        IsBusy = true;
        try
        {
            if (!_host.SaveAllEditors())
            {
                ErrorMessage = "Kunne ikke gemme alle dokumenter før aktivering.";
                return;
            }

            var result = await _versionControlService.EnableAsync(workspace, CancellationToken.None);
            if (!result.Success)
            {
                ErrorMessage = result.Message;
                return;
            }

            InfoMessage = "Versionsstyring er slået til for arbejdsmappen.";
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync();
        }
    }

    private async Task ShowWorkingChangesAsync()
    {
        ErrorMessage = await _workflow.ShowWorkingChangesAsync(_host);
        if (ErrorMessage is null)
        {
            InfoMessage = null;
        }
    }

    private async Task CommitAllAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;
        await _workflow.CommitAllAsync(_host);
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var previous = Interlocked.Exchange(ref _refreshCancellation, new CancellationTokenSource());
        previous?.Cancel();
        previous?.Dispose();
        var ct = _refreshCancellation!.Token;
        IsBusy = true;
        try
        {
            var workspace = _host.WorkspaceFolder;
            if (string.IsNullOrWhiteSpace(workspace))
            {
                ApplyNoWorkspaceState();
                return;
            }

            var status = await _versionControlService.GetStatusAsync(workspace, ct);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            ApplyStatus(status);
            Entries.Clear();
            HistoryNote = null;
            if (status.State != VersionControlState.Klar)
            {
                return;
            }

            var history = await _versionControlService.GetHistoryAsync(workspace, 500, ct);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            for (var i = 0; i < history.Entries.Count; i++)
            {
                var entry = history.Entries[i];
                var disableRollback = i == 0 && status.ChangedFileCount == 0;
                Entries.Add(new VersionEntryViewModel(
                    entry,
                    async () =>
                    {
                        ErrorMessage = await _workflow.ShowCommitChangesAsync(_host, entry);
                        if (ErrorMessage is null)
                        {
                            InfoMessage = null;
                        }
                    },
                    async () =>
                    {
                        ErrorMessage = null;
                        InfoMessage = null;
                        var result = await _workflow.RollbackAsync(_host, entry);
                        if (result is null)
                        {
                            return;
                        }

                        if (result.Success)
                        {
                            InfoMessage = string.Equals(result.Message, "Dokumenterne er allerede i denne tilstand.", StringComparison.Ordinal)
                                ? result.Message
                                : $"Dokumenterne er rullet tilbage til {entry.Time.ToLocalTime():dd-MM-yyyy HH:mm}.";
                        }
                        else
                        {
                            ErrorMessage = result.Message;
                        }

                        await RefreshAsync();
                    },
                    !disableRollback));
            }

            HistoryNote = history.HasMore ? "Viser de seneste 500 versioner." : null;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsBusy = false;
            UpdateCommandState();
        }
    }

    private void ApplyNoWorkspaceState()
    {
        Entries.Clear();
        StatusText = "Vælg først en arbejdsmappe i Indstillinger.";
        ExtraStatusText = null;
        ShowEnableButton = false;
        HasChanges = false;
        CanShowWorkingChanges = false;
        CanCommitAll = false;
    }

    private void ApplyStatus(VersionStatus status)
    {
        ShowEnableButton = status.State == VersionControlState.IkkeAktiveret;
        HasChanges = status.ChangedFileCount > 0;
        CanShowWorkingChanges = status.State == VersionControlState.Klar && HasChanges;
        CanCommitAll = status.State == VersionControlState.Klar && HasChanges;

        ExtraStatusText = _host.HasUnsavedEditors
            ? "Der er ugemte ændringer i editoren. De gemmes automatisk, når du committer."
            : null;

        StatusText = status.State switch
        {
            VersionControlState.GitMangler => "Git er ikke installeret, så versionsstyring er ikke tilgængelig.",
            VersionControlState.IkkeAktiveret => "Versionsstyring er ikke slået til for denne mappe.",
            VersionControlState.ForkertArkiv => "Mappen ligger inde i et andet Git-arkiv og kan ikke versioneres her.",
            VersionControlState.Klar when status.ChangedFileCount == 0 => "Ingen ikke committede ændringer.",
            VersionControlState.Klar => BuildChangeStatusText(status),
            _ => string.Empty,
        };
    }

    private static string BuildChangeStatusText(VersionStatus status)
    {
        var countText = status.ChangedFileCount == 1
            ? "1 ikke committet ændring (dokument)"
            : string.Format(CultureInfo.InvariantCulture, "{0} ikke committede ændringer (dokumenter)", status.ChangedFileCount);
        var lastText = status.LastChangeTime.HasValue
            ? status.LastChangeTime.Value.ToLocalTime().ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
            : "tidspunkt ukendt";
        return $"{countText}. Seneste ændring: {lastText}.";
    }

    private void HandleDocumentsSaved(object? sender, EventArgs e)
    {
        _ = RefreshAsync();
    }

    partial void OnIsBusyChanged(bool value)
    {
        UpdateCommandState();
    }

    private void UpdateCommandState()
    {
        ShowChangesCommand.NotifyCanExecuteChanged();
        CommitAllCommand.NotifyCanExecuteChanged();
        EnableCommand.NotifyCanExecuteChanged();
    }
}

public sealed class VersionEntryViewModel
{
    public VersionEntryViewModel(
        VersionEntry entry,
        Func<Task> showChanges,
        Func<Task> rollback,
        bool canRollback)
    {
        Entry = entry;
        DateText = entry.Time.ToLocalTime().ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        TimeText = entry.Time.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
        Message = entry.Message;
        IsRollbackEnabled = canRollback;
        ShowChangesCommand = new AsyncRelayCommand(showChanges);
        RollbackCommand = new AsyncRelayCommand(rollback, () => IsRollbackEnabled);
    }

    public VersionEntry Entry { get; }
    public string Id => Entry.Id;
    public string DateText { get; }
    public string TimeText { get; }
    public string Message { get; }
    public bool IsRollbackVisible => true;
    public bool IsRollbackEnabled { get; }
    public IAsyncRelayCommand ShowChangesCommand { get; }
    public IAsyncRelayCommand RollbackCommand { get; }
}
