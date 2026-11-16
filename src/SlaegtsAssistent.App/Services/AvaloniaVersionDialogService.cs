using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaVersionDialogService : IVersionDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaVersionDialogService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<string?> AskCommitMessageAsync(string suggestedMessage)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return null;
        }

        var result = (string?)null;
        var textBox = new TextBox
        {
            Text = suggestedMessage,
            MinWidth = 420,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        AutomationProperties.SetName(textBox, "Commitbesked");

        var commitButton = new Button
        {
            Content = "Commit",
            MinWidth = 90,
            IsDefault = true,
        };
        AutomationProperties.SetName(commitButton, "Commit");

        var cancelButton = new Button
        {
            Content = "Annullér",
            MinWidth = 90,
            IsCancel = true,
        };
        AutomationProperties.SetName(cancelButton, "Annullér");

        var dialog = new Window
        {
            Title = "Commit",
            Width = 560,
            Height = 230,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Beskriv kort, hvad der er ændret:",
                    },
                    textBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children =
                        {
                            cancelButton,
                            commitButton,
                        },
                    },
                },
            },
        };

        void UpdateButtonState()
            => commitButton.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);

        UpdateButtonState();
        textBox.AttachedToVisualTree += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };
        textBox.PropertyChanged += (_, args) =>
        {
            if (args.Property == TextBox.TextProperty)
            {
                UpdateButtonState();
            }
        };
        commitButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                return;
            }

            result = textBox.Text.Trim();
            dialog.Close();
        };
        cancelButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
        return result;
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return false;
        }

        var result = false;
        var confirmButton = new Button
        {
            Content = confirmText,
            MinWidth = 120,
            IsDefault = true,
        };
        AutomationProperties.SetName(confirmButton, confirmText);

        var cancelButton = new Button
        {
            Content = "Annullér",
            MinWidth = 90,
            IsCancel = true,
        };
        AutomationProperties.SetName(cancelButton, "Annullér");

        var dialog = new Window
        {
            Title = title,
            Width = 560,
            Height = 240,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children =
                        {
                            cancelButton,
                            confirmButton,
                        },
                    },
                },
            },
        };

        confirmButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        cancelButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
        return result;
    }
}
