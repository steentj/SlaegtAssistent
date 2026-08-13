using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly ITemplateFilePickerService _templateFilePickerService;
    private readonly Person? _previewPerson;
    private readonly string? _gedcomFilePath;
    private readonly string? _previewOutputFolder;

    public SettingsWindowViewModel(
        AppSettings currentSettings,
        IFolderPickerService folderPickerService,
        ITemplateFilePickerService? templateFilePickerService = null,
        Person? previewPerson = null,
        string? gedcomFilePath = null,
        string? previewOutputFolder = null)
    {
        _folderPickerService = folderPickerService;
        _templateFilePickerService = templateFilePickerService ?? new NullTemplateFilePickerService();
        _previewPerson = previewPerson;
        _gedcomFilePath = gedcomFilePath;
        _previewOutputFolder = previewOutputFolder;
        DefaultGedcomInputFolder = currentSettings.DefaultGedcomInputFolder;
        DefaultMarkdownOutputFolder = currentSettings.DefaultMarkdownOutputFolder;
        GlobalBiographyTemplatePath = currentSettings.GlobalBiographyTemplatePath;
        Theme = currentSettings.Theme;
    }

    [ObservableProperty]
    private string? defaultGedcomInputFolder;

    [ObservableProperty]
    private string? defaultMarkdownOutputFolder;

    [ObservableProperty]
    private string? globalBiographyTemplatePath;

    [ObservableProperty]
    private string previewText = string.Empty;

    [ObservableProperty]
    private string? templateErrorMessage;

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
    private async Task SelectBiographyTemplateAsync()
    {
        var selectedFile = await _templateFilePickerService.PickTemplateFileAsync(
            Path.GetDirectoryName(GlobalBiographyTemplatePath ?? string.Empty)
                ?? DefaultMarkdownOutputFolder);

        if (!string.IsNullOrWhiteSpace(selectedFile))
        {
            GlobalBiographyTemplatePath = selectedFile;
        }
    }

    [RelayCommand]
    private void ResetBiographyTemplate()
    {
        GlobalBiographyTemplatePath = null;
    }

    [RelayCommand]
    private void PreviewBiographyTemplate()
    {
        if (_previewPerson is null)
        {
            TemplateErrorMessage = "Indlæs en GEDCOM-fil og vælg en person for at se en forhåndsvisning.";
            PreviewText = string.Empty;
            return;
        }

        try
        {
            var template = string.IsNullOrWhiteSpace(GlobalBiographyTemplatePath)
                ? BiographyTemplateMarkdownGenerator.DefaultTemplate
                : File.ReadAllText(GlobalBiographyTemplatePath);
            PreviewText = new BiographyTemplateRenderer().Render(
                new BiographyTemplateLoader().Parse(template, GlobalBiographyTemplatePath),
                BiographyTemplateContext.FromPerson(
                    _previewPerson,
                    mediaBaseDirectory: _previewOutputFolder,
                    gedcomSourceDirectory: string.IsNullOrWhiteSpace(_gedcomFilePath)
                        ? null
                        : Path.GetDirectoryName(_gedcomFilePath)));
            TemplateErrorMessage = null;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or BiographyTemplateException)
        {
            PreviewText = string.Empty;
            TemplateErrorMessage = $"Skabelonen kunne ikke forhåndsvises: {exception.Message}";
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (!ValidateSelectedTemplate())
        {
            return;
        }

        CloseRequested?.Invoke(this, new AppSettings
        {
            DefaultGedcomInputFolder = NormalizeFolder(DefaultGedcomInputFolder),
            DefaultMarkdownOutputFolder = NormalizeFolder(DefaultMarkdownOutputFolder),
            GlobalBiographyTemplatePath = NormalizePath(GlobalBiographyTemplatePath),
            Theme = Theme,
        });
    }

    private bool ValidateSelectedTemplate()
    {
        var path = NormalizePath(GlobalBiographyTemplatePath);
        if (path is null)
        {
            TemplateErrorMessage = null;
            return true;
        }

        try
        {
            new BiographyTemplateLoader().Load(path);
            TemplateErrorMessage = null;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or BiographyTemplateException)
        {
            TemplateErrorMessage = $"Skabelonen kan ikke gemmes som aktiv: {exception.Message}";
            return false;
        }
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

    private static string? NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
    }

    private sealed class NullTemplateFilePickerService : ITemplateFilePickerService
    {
        public Task<string?> PickTemplateFileAsync(string? suggestedStartFolder)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
