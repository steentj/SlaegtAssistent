using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaTemplateCheatSheetService : ITemplateCheatSheetService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private Window? _window;

    public AvaloniaTemplateCheatSheetService(
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
            ((Views.TemplateCheatSheetWindow)_window).FocusSearch();
            return;
        }

        var window = new Views.TemplateCheatSheetWindow
        {
            DataContext = new TemplateCheatSheetViewModel(),
        };
        _window = window;
        window.Closed += (_, _) => _window = null;
        window.Show(owner);
        window.FocusSearch();
    }
}
