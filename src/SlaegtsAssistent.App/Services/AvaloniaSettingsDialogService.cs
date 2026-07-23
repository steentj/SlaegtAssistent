using Avalonia.Controls.ApplicationLifetimes;
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
        return await dialog.ShowDialog<AppSettings?>(owner);
    }
}
