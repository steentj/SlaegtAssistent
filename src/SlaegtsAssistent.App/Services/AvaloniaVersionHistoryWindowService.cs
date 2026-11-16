using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaVersionHistoryWindowService : IVersionHistoryWindowService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private readonly IVersionControlService _versionControlService;
    private readonly VersioningWorkflow _workflow;
    private Window? _window;

    public AvaloniaVersionHistoryWindowService(
        IClassicDesktopStyleApplicationLifetime applicationLifetime,
        IVersionControlService versionControlService,
        VersioningWorkflow workflow)
    {
        _applicationLifetime = applicationLifetime;
        _versionControlService = versionControlService;
        _workflow = workflow;
    }

    public void Show(IVersioningHost host)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return;
        }

        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        var window = new VersionHistoryWindow
        {
            DataContext = new VersionHistoryViewModel(host, _versionControlService, _workflow),
        };
        _window = window;
        window.Closed += (_, _) => _window = null;
        window.Show(owner);
    }
}
