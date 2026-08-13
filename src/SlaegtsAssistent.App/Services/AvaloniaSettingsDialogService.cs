using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.Styling;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Domain;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaSettingsDialogService : ISettingsDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private readonly IFolderPickerService _folderPickerService;
    private readonly ITemplateFilePickerService _templateFilePickerService;

    public AvaloniaSettingsDialogService(
        IClassicDesktopStyleApplicationLifetime applicationLifetime,
        IFolderPickerService folderPickerService,
        ITemplateFilePickerService templateFilePickerService)
    {
        _applicationLifetime = applicationLifetime;
        _folderPickerService = folderPickerService;
        _templateFilePickerService = templateFilePickerService;
    }

    public async Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings)
    {
        return await EditSettingsAsync(currentSettings, null);
    }

    public async Task<AppSettings?> EditSettingsAsync(
        AppSettings currentSettings,
        Person? previewPerson)
    {
        return await EditSettingsAsync(currentSettings, previewPerson, null, null);
    }

    public async Task<AppSettings?> EditSettingsAsync(
        AppSettings currentSettings,
        Person? previewPerson,
        string? gedcomFilePath,
        string? outputFolder)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return null;
        }

        var viewModel = new SettingsWindowViewModel(
            currentSettings,
            _folderPickerService,
            _templateFilePickerService,
            previewPerson,
            gedcomFilePath,
            outputFolder);
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
