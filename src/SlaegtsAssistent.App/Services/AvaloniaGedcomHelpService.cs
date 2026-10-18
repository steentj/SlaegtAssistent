using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaGedcomHelpService : IGedcomHelpService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private Window? _window;

    public AvaloniaGedcomHelpService(
        IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public void Show()
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

        var window = new Views.GedcomHelpWindow
        {
            DataContext = new GedcomHelpViewModel(),
        };
        _window = window;
        window.Closed += (_, _) => _window = null;
        window.Show(owner);
    }
}
