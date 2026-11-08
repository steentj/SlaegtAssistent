using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaTemplateEditorConfirmationService : ITemplateEditorConfirmationService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaTemplateEditorConfirmationService(
        IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<bool> ConfirmResetAsync()
    {
        var owner = AvaloniaPickerWindowResolver.Resolve(_applicationLifetime);
        if (owner is null)
        {
            return false;
        }

        var result = false;
        var resetButton = new Button
        {
            Content = "Nulstil",
            MinWidth = 90,
            IsDefault = true,
        };
        var cancelButton = new Button
        {
            Content = "Behold ændringer",
            MinWidth = 130,
            IsCancel = true,
        };
        var dialog = new Window
        {
            Title = "Nulstil skabelonen?",
            Width = 460,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        resetButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        cancelButton.Click += (_, _) => dialog.Close();
        dialog.Content = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 20,
            Children =
            {
                new TextBlock
                {
                    Text = "Alle ændringer i denne redigeringssession erstattes med standardskabelonen.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                new StackPanel
                {
                    [Grid.RowProperty] = 1,
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, resetButton },
                },
            },
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
