using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaMarkdownCheatSheetService : IMarkdownCheatSheetService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private Window? _window;

    public AvaloniaMarkdownCheatSheetService(
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

        _window = new Views.MarkdownCheatSheetWindow
        {
            DataContext = new MarkdownCheatSheetViewModel(),
        };
        _window.Closed += (_, _) => _window = null;
        _window.Show(owner);
        _window.Activate();
    }
}
