using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly IFolderPickerService _folderPickerService;

    public SettingsWindowViewModel(AppSettings currentSettings, IFolderPickerService folderPickerService)
    {
        _folderPickerService = folderPickerService;
        DefaultGedcomInputFolder = currentSettings.DefaultGedcomInputFolder;
        DefaultMarkdownOutputFolder = currentSettings.DefaultMarkdownOutputFolder;
        Theme = currentSettings.Theme;
    }

    [ObservableProperty]
    private string? defaultGedcomInputFolder;

    [ObservableProperty]
    private string? defaultMarkdownOutputFolder;

    [ObservableProperty]
    private ThemePreference theme;

    public IReadOnlyList<string> ThemeOptions { get; } =
        ["Systemstandard", "Lyst", "Mørkt"];

    public string SelectedTheme
    {
        get => Theme switch
        {
            ThemePreference.Light => "Lyst",
            ThemePreference.Dark => "Mørkt",
            _ => "Systemstandard",
        };
        set
        {
            Theme = value switch
            {
                "Lyst" => ThemePreference.Light,
                "Mørkt" => ThemePreference.Dark,
                _ => ThemePreference.System,
            };
            OnPropertyChanged();
        }
    }

    public event EventHandler<AppSettings?>? CloseRequested;

    [RelayCommand]
    private async Task SelectGedcomInputFolderAsync()
    {
        var selectedFolder = await _folderPickerService.PickFolderAsync(
            "Vælg standardmappe for GEDCOM-filer",
            DefaultGedcomInputFolder);

        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            DefaultGedcomInputFolder = selectedFolder;
        }
    }

    [RelayCommand]
    private async Task SelectMarkdownOutputFolderAsync()
    {
        var selectedFolder = await _folderPickerService.PickFolderAsync(
            "Vælg standardmappe for Markdown-filer",
            DefaultMarkdownOutputFolder ?? DefaultGedcomInputFolder);

        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            DefaultMarkdownOutputFolder = selectedFolder;
        }
    }

    [RelayCommand]
    private void Save()
    {
        CloseRequested?.Invoke(this, new AppSettings
        {
            DefaultGedcomInputFolder = NormalizeFolder(DefaultGedcomInputFolder),
            DefaultMarkdownOutputFolder = NormalizeFolder(DefaultMarkdownOutputFolder),
            Theme = Theme,
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, null);
    }

    private static string? NormalizeFolder(string? folder)
    {
        return string.IsNullOrWhiteSpace(folder) ? null : folder.Trim();
    }
}
