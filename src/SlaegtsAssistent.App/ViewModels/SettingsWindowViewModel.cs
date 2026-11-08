using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly ITemplateEditorDialogService? _templateEditorDialogService;
    private readonly TemplateEditorStorageService _templateStorage;
    private BiographyTemplateEditorModel? _pendingTemplate;
    private readonly IReadOnlyList<Person> _previewPeople;

    public SettingsWindowViewModel(
        AppSettings currentSettings,
        IFolderPickerService folderPickerService,
        ITemplateFilePickerService? templateFilePickerService = null,
        Person? previewPerson = null,
        string? gedcomFilePath = null,
        string? previewOutputFolder = null,
        ITemplateEditorDialogService? templateEditorDialogService = null,
        TemplateEditorStorageService? templateStorage = null,
        IEnumerable<Person>? previewPeople = null)
    {
        _folderPickerService = folderPickerService;
        _templateFilePickerService = templateFilePickerService ?? new NullTemplateFilePickerService();
        _previewPerson = previewPerson;
        _gedcomFilePath = gedcomFilePath;
        _previewOutputFolder = previewOutputFolder;
        _templateEditorDialogService = templateEditorDialogService;
        _templateStorage = templateStorage ?? new TemplateEditorStorageService();
        _previewPeople = previewPeople?.ToArray() ?? (previewPerson is null ? [] : [previewPerson]);
        DefaultGedcomInputFolder = currentSettings.DefaultGedcomInputFolder;
        DefaultMarkdownOutputFolder = currentSettings.DefaultMarkdownOutputFolder;
        GlobalBiographyTemplatePath = currentSettings.GlobalBiographyTemplatePath;
        Theme = currentSettings.Theme;
        CheckSelectedTemplate();
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
    private string? templateStatusMessage;

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
        _pendingTemplate = null;
        TemplateStatusMessage = "Standardskabelonen aktiveres, når du gemmer Indstillinger.";
    }

    partial void OnGlobalBiographyTemplatePathChanged(string? value)
    {
        _pendingTemplate = null;
        TemplateStatusMessage = null;
        CheckSelectedTemplate();
    }

    private void CheckSelectedTemplate()
    {
        try
        {
            if (NormalizePath(GlobalBiographyTemplatePath) is { } path)
            {
                _templateStorage.Load(path);
            }
            TemplateErrorMessage = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BiographyTemplateException)
        {
            TemplateErrorMessage = exception.Message;
        }
    }

    [RelayCommand]
    private async Task EditBiographyTemplateAsync()
    {
        if (_templateEditorDialogService is null)
        {
            return;
        }

        var model = _pendingTemplate;
        var startNew = false;
        if (model is null)
        {
            try
            {
                model = NormalizePath(GlobalBiographyTemplatePath) is { } path
                    ? _templateStorage.Load(path)
                    : BiographyTemplateEditorModel.CreateDefault();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BiographyTemplateException)
            {
                TemplateErrorMessage = exception.Message;
                model = BiographyTemplateEditorModel.CreateDefault();
                startNew = true;
            }
        }

        var edited = await _templateEditorDialogService.EditAsync(model, _previewPeople);
        if (edited is not null)
        {
            if (startNew)
            {
                GlobalBiographyTemplatePath = null;
            }
            _pendingTemplate = edited;
            TemplateStatusMessage = "Skabelonen er en kladde. Gem eller Gem som i Indstillinger skriver filen og aktiverer den. Annuller bevarer alle filer og indstillinger.";
        }
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
            var template = _pendingTemplate is not null
                ? new BiographyTemplateEditorCompiler().Compile(_pendingTemplate)
                : string.IsNullOrWhiteSpace(GlobalBiographyTemplatePath)
                    ? BiographyTemplateMarkdownGenerator.DefaultTemplate
                    : new BiographyTemplateLoader().Load(GlobalBiographyTemplatePath).Source;
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
                or BiographyTemplateException
                or BiographyTemplateEditorException)
        {
            PreviewText = string.Empty;
            TemplateErrorMessage = $"Skabelonen kunne ikke forhåndsvises: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveSettingsAsync(false);
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        await SaveSettingsAsync(true);
    }

    private async Task SaveSettingsAsync(bool saveAs)
    {
        if (_pendingTemplate is not null || saveAs)
        {
            try
            {
                var model = _pendingTemplate ?? (NormalizePath(GlobalBiographyTemplatePath) is { } existingPath
                    ? _templateStorage.Load(existingPath)
                    : BiographyTemplateEditorModel.CreateDefault());
                var path = NormalizePath(GlobalBiographyTemplatePath);
                if (saveAs || path is null)
                {
                    path = await _templateFilePickerService.PickTemplateSaveFileAsync(path);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return;
                    }
                }
                _templateStorage.Save(path, model);
                GlobalBiographyTemplatePath = path;
                _pendingTemplate = null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                BiographyTemplateException or BiographyTemplateEditorException or ArgumentException)
            {
                TemplateErrorMessage = $"Skabelonen kunne ikke gemmes. Den tidligere aktive skabelon er bevaret: {exception.Message}";
                return;
            }
        }

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
