using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.App.Services;

public sealed class VersioningWorkflow(
    IVersionControlService versionControlService,
    IVersionDialogService versionDialogService,
    IUserDialogService userDialogService,
    IVersionDiffWindowService versionDiffWindowService)
{
    private static readonly SemaphoreSlim WorkspaceLock = new(1, 1);
    private readonly IVersionControlService _versionControlService = versionControlService;
    private readonly IVersionDialogService _versionDialogService = versionDialogService;
    private readonly IUserDialogService _userDialogService = userDialogService;
    private readonly IVersionDiffWindowService _versionDiffWindowService = versionDiffWindowService;

    public async Task<bool> CommitAllAsync(IVersioningHost host)
    {
        if (host.IsBusyWithImport)
        {
            await _userDialogService.ShowInformationAsync("Commit", "Commit er deaktiveret under import.");
            return false;
        }

        var workspace = host.WorkspaceFolder;
        if (string.IsNullOrWhiteSpace(workspace))
        {
            await _userDialogService.ShowInformationAsync("Commit", "Vælg først en arbejdsmappe i Indstillinger.");
            return false;
        }

        var status = await _versionControlService.GetStatusAsync(workspace, CancellationToken.None);
        if (status.State == VersionControlState.GitMangler || status.State == VersionControlState.ForkertArkiv)
        {
            await _userDialogService.ShowInformationAsync("Versionsstyring", status.Message ?? "Versionsstyring er ikke tilgængelig.");
            return false;
        }

        if (status.State == VersionControlState.IkkeAktiveret)
        {
            var confirm = await _versionDialogService.ConfirmAsync(
                    "Slå versionsstyring til",
                    "Mappen er ikke versioneret endnu. Vil du slå versionsstyring til nu?",
                    "Slå til");
            if (!confirm)
            {
                return false;
            }

            if (!host.SaveAllEditors())
            {
                return false;
            }

            var enable = await _versionControlService.EnableAsync(workspace, CancellationToken.None);
            if (!enable.Success)
            {
                await _userDialogService.ShowInformationAsync("Versionsstyring", enable.Message ?? "Kunne ikke slå versionsstyring til.");
                return false;
            }

            status = await _versionControlService.GetStatusAsync(workspace, CancellationToken.None);
        }
        else
        {
            if (!host.SaveAllEditors())
            {
                return false;
            }

            status = await _versionControlService.GetStatusAsync(workspace, CancellationToken.None);
        }

        if (status.ChangedFileCount == 0)
        {
            await _userDialogService.ShowInformationAsync("Commit", "Der er ingen ændringer at committe.");
            return false;
        }

        var message = await _versionDialogService.AskCommitMessageAsync(CommitMessageSuggester.Suggest(DateTimeOffset.Now));
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var result = await _versionControlService.CommitAllAsync(workspace, message, CancellationToken.None);
        if (!result.Success)
        {
            await _userDialogService.ShowInformationAsync("Commit", result.Message ?? "Commit kunne ikke gennemføres.");
            return false;
        }

        return true;
    }

    public async Task<VersionOperationResult?> RollbackAsync(IVersioningHost host, VersionEntry entry)
    {
        if (host.IsBusyWithImport)
        {
            return new VersionOperationResult(false, "Tilbagerulning er deaktiveret under import.");
        }

        var workspace = host.WorkspaceFolder;
        if (string.IsNullOrWhiteSpace(workspace))
        {
            return new VersionOperationResult(false, "Vælg først en arbejdsmappe i Indstillinger.");
        }

        var confirmationMessage =
            $"Vil du rulle dokumenterne tilbage til {entry.Time.ToLocalTime():dd-MM-yyyy HH:mm}?\n\n" +
            $"\"{entry.Message}\"\n\n" +
            "Den nuværende tilstand gemmes som en ny version først.";
        var confirm = await _versionDialogService.ConfirmAsync("Rul tilbage her", confirmationMessage, "Rul tilbage");
        if (!confirm)
        {
            return null;
        }

        await WorkspaceLock.WaitAsync(CancellationToken.None);
        try
        {
            if (!host.SaveAllEditors())
            {
                return new VersionOperationResult(false, "Kunne ikke gemme alle åbne dokumenter før tilbagerulning.");
            }

            var result = await _versionControlService.RollbackToAsync(workspace, entry.Id, CancellationToken.None);
            if (!result.Success)
            {
                return result;
            }

            if (!string.Equals(result.Message, "Dokumenterne er allerede i denne tilstand.", StringComparison.Ordinal))
            {
                host.ReloadWorkspace();
            }

            return result;
        }
        finally
        {
            WorkspaceLock.Release();
        }
    }

    public async Task<string?> ShowWorkingChangesAsync(IVersioningHost host)
    {
        var workspace = host.WorkspaceFolder;
        if (string.IsNullOrWhiteSpace(workspace))
        {
            return "Vælg først en arbejdsmappe i Indstillinger.";
        }

        try
        {
            var files = await _versionControlService.GetWorkingChangesAsync(workspace, CancellationToken.None);
            _versionDiffWindowService.Show("Ikke committede ændringer", files, null);
            return null;
        }
        catch (VersionControlException exception)
        {
            return exception.Message;
        }
    }

    public async Task<string?> ShowCommitChangesAsync(IVersioningHost host, VersionEntry entry)
    {
        var workspace = host.WorkspaceFolder;
        if (string.IsNullOrWhiteSpace(workspace))
        {
            return "Vælg først en arbejdsmappe i Indstillinger.";
        }

        try
        {
            var files = await _versionControlService.GetCommitChangesAsync(workspace, entry.Id, CancellationToken.None);
            var title = string.Format(
                CultureInfo.InvariantCulture,
                "Ændringer {0:dd-MM-yyyy HH:mm}",
                entry.Time.ToLocalTime());
            _versionDiffWindowService.Show(title, files, null);
            return null;
        }
        catch (VersionControlException exception)
        {
            return exception.Message;
        }
    }
}
