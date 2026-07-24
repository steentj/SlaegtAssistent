using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaUnsavedChangesDialogService : IUnsavedChangesDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaUnsavedChangesDialogService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<UnsavedChangesDecision> AskAsync()
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return UnsavedChangesDecision.Annullér;
        }

        var result = UnsavedChangesDecision.Annullér;

        var saveButton = new Button
        {
            Content = "Gem",
            MinWidth = 90,
        };

        var discardButton = new Button
        {
            Content = "Kassér",
            MinWidth = 90,
        };

        var cancelButton = new Button
        {
            Content = "Annullér",
            MinWidth = 90,
        };

        var dialog = new Window
        {
            Title = "Ugemte ændringer",
            Width = 520,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Der er ugemte ændringer. Hvad vil du gøre?",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children =
                        {
                            cancelButton,
                            discardButton,
                            saveButton,
                        },
                    },
                },
            },
        };

        saveButton.Click += (_, _) =>
        {
            result = UnsavedChangesDecision.Gem;
            dialog.Close();
        };

        discardButton.Click += (_, _) =>
        {
            result = UnsavedChangesDecision.Kassér;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
        return result;
    }
}
