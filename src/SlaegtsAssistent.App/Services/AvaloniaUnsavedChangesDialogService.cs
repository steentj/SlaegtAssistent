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

        var dialogState = CreateDialog();
        await dialogState.Dialog.ShowDialog(owner);
        return dialogState.Decision;
    }

    internal static UnsavedChangesDialogState CreateDialog()
    {
        var dialogState = new UnsavedChangesDialogState();

        var saveButton = new Button
        {
            Content = "Gem",
            MinWidth = 90,
            IsDefault = true,
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
            IsCancel = true,
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
            dialogState.Decision = UnsavedChangesDecision.Gem;
            dialog.Close();
        };

        discardButton.Click += (_, _) =>
        {
            dialogState.Decision = UnsavedChangesDecision.Kassér;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        dialogState.Dialog = dialog;
        return dialogState;
    }
}

internal sealed class UnsavedChangesDialogState
{
    public Window Dialog { get; set; } = null!;

    public UnsavedChangesDecision Decision { get; set; } = UnsavedChangesDecision.Annullér;
}
