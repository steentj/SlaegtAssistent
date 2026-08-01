using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.Styling;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaSettingsDialogService : ISettingsDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private readonly IFolderPickerService _folderPickerService;

    public AvaloniaSettingsDialogService(
        IClassicDesktopStyleApplicationLifetime applicationLifetime,
        IFolderPickerService folderPickerService)
    {
        _applicationLifetime = applicationLifetime;
        _folderPickerService = folderPickerService;
    }

    public async Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return null;
        }

        var viewModel = new SettingsWindowViewModel(currentSettings, _folderPickerService);
        var dialog = new SettingsWindow(viewModel);
        var result = await dialog.ShowDialog<AppSettings?>(owner);
        if (result is not null && Application.Current is { } application)
        {
            application.RequestedThemeVariant = result.Theme switch
            {
                ThemePreference.Light => ThemeVariant.Light,
                ThemePreference.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }

        return result;
    }
}
