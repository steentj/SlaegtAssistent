using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.App.Tests;

public sealed class VersioningWorkflowTests
{
    [Fact]
    public async Task CommitAllAsync_UdenArbejdsmappe_ViserFejlOgStopper()
    {
        var dialogs = new RecordingUserDialogService();
        var workflow = new VersioningWorkflow(
            new FakeVersionControlService(),
            new FakeVersionDialogService(),
            dialogs,
            new NullVersionDiffWindowService());
        var host = new FakeVersioningHost { WorkspaceFolder = null };

        var success = await workflow.CommitAllAsync(host);

        success.Should().BeFalse();
        dialogs.Messages.Should().ContainSingle(message => message.message.Contains("Vælg først en arbejdsmappe"));
    }

    [Fact]
    public async Task CommitAllAsync_GemmerFoerAktiveringOgCommit()
    {
        var service = new FakeVersionControlService
        {
            Statuses =
            [
                new VersionStatus(VersionControlState.IkkeAktiveret, null, [], null),
                new VersionStatus(VersionControlState.Klar, null, [new WorkingTreeChange("a.md", WorkingTreeChangeKind.Aendret)], DateTimeOffset.Now),
            ],
            EnableResult = new VersionOperationResult(true, null),
            CommitResult = new VersionOperationResult(true, null),
        };
        var dialog = new FakeVersionDialogService
        {
            ConfirmResult = true,
            CommitMessage = "Min commit",
        };
        var workflow = new VersioningWorkflow(service, dialog, new RecordingUserDialogService(), new NullVersionDiffWindowService());
        var host = new FakeVersioningHost { WorkspaceFolder = "/tmp/test" };

        var success = await workflow.CommitAllAsync(host);

        success.Should().BeTrue();
        host.SaveAllCalls.Should().Be(1);
        service.EnableCalls.Should().Be(1);
        service.CommitCalls.Should().Be(1);
        service.LastCommitMessage.Should().Be("Min commit");
    }

    [Fact]
    public async Task CommitAllAsync_UdenAendringer_OpretterIngenCommit()
    {
        var service = new FakeVersionControlService
        {
            Statuses =
            [
                new VersionStatus(VersionControlState.Klar, null, [], null),
            ],
        };
        var userDialog = new RecordingUserDialogService();
        var workflow = new VersioningWorkflow(service, new FakeVersionDialogService(), userDialog, new NullVersionDiffWindowService());
        var host = new FakeVersioningHost { WorkspaceFolder = "/tmp/test" };

        var success = await workflow.CommitAllAsync(host);

        success.Should().BeFalse();
        service.CommitCalls.Should().Be(0);
        userDialog.Messages.Should().ContainSingle(message => message.message.Contains("ingen ændringer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RollbackAsync_GemmerFoerRollbackOgGenindlaeserVedSucces()
    {
        var service = new FakeVersionControlService
        {
            RollbackResult = new VersionOperationResult(true, "OK"),
        };
        var dialog = new FakeVersionDialogService { ConfirmResult = true };
        var workflow = new VersioningWorkflow(service, dialog, new RecordingUserDialogService(), new NullVersionDiffWindowService());
        var host = new FakeVersioningHost { WorkspaceFolder = "/tmp/test" };
        var entry = new VersionEntry(new string('a', 40), DateTimeOffset.Now, "Besked");

        var result = await workflow.RollbackAsync(host, entry);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        host.SaveAllCalls.Should().Be(1);
        host.ReloadCalls.Should().Be(1);
        service.RollbackCalls.Should().Be(1);
    }

    private sealed class FakeVersioningHost : IVersioningHost
    {
        public string? WorkspaceFolder { get; set; }
        public bool IsBusyWithImport { get; set; }
        public bool HasUnsavedEditors { get; set; }
        public int SaveAllCalls { get; private set; }
        public int ReloadCalls { get; private set; }
        public event EventHandler? DocumentsSaved;

        public bool SaveAllEditors()
        {
            SaveAllCalls++;
            return true;
        }

        public void ReloadWorkspace()
        {
            ReloadCalls++;
        }

        public void RaiseDocumentsSaved() => DocumentsSaved?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FakeVersionDialogService : IVersionDialogService
    {
        public bool ConfirmResult { get; set; } = true;
        public string? CommitMessage { get; set; } = "Test";

        public Task<string?> AskCommitMessageAsync(string suggestedMessage)
            => Task.FromResult(CommitMessage);

        public Task<bool> ConfirmAsync(string title, string message, string confirmText)
            => Task.FromResult(ConfirmResult);
    }

    private sealed class RecordingUserDialogService : IUserDialogService
    {
        public List<(string title, string message)> Messages { get; } = [];

        public Task ShowInformationAsync(string title, string message)
        {
            Messages.Add((title, message));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVersionControlService : IVersionControlService
    {
        private int _statusIndex;

        public List<VersionStatus> Statuses { get; set; } = [];
        public VersionOperationResult EnableResult { get; set; } = new(true, null);
        public VersionOperationResult CommitResult { get; set; } = new(true, null);
        public VersionOperationResult RollbackResult { get; set; } = new(true, null);
        public int EnableCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }
        public string? LastCommitMessage { get; private set; }

        public Task<VersionStatus> GetStatusAsync(string workspace, CancellationToken ct)
        {
            if (_statusIndex < Statuses.Count)
            {
                return Task.FromResult(Statuses[_statusIndex++]);
            }

            return Task.FromResult(new VersionStatus(VersionControlState.Klar, null, [], null));
        }

        public Task<VersionOperationResult> EnableAsync(string workspace, CancellationToken ct)
        {
            EnableCalls++;
            return Task.FromResult(EnableResult);
        }

        public Task<VersionHistory> GetHistoryAsync(string workspace, int maxEntries, CancellationToken ct)
            => Task.FromResult(new VersionHistory([], false));

        public Task<VersionOperationResult> CommitAllAsync(string workspace, string message, CancellationToken ct)
        {
            CommitCalls++;
            LastCommitMessage = message;
            return Task.FromResult(CommitResult);
        }

        public Task<IReadOnlyList<DiffFile>> GetWorkingChangesAsync(string workspace, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DiffFile>>([]);

        public Task<IReadOnlyList<DiffFile>> GetCommitChangesAsync(string workspace, string commitId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DiffFile>>([]);

        public Task<VersionOperationResult> RollbackToAsync(string workspace, string commitId, CancellationToken ct)
        {
            RollbackCalls++;
            return Task.FromResult(RollbackResult);
        }
    }
}
