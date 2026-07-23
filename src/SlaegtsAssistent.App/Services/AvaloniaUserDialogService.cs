using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaUserDialogService : IUserDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaUserDialogService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task ShowInformationAsync(string title, string message)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return;
        }

        var closeButton = new Button
        {
            Content = "Luk",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 90,
        };

        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = 260,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DockPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    closeButton,
                },
            },
        };

        DockPanel.SetDock(closeButton, Dock.Bottom);
        closeButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
    }
}
