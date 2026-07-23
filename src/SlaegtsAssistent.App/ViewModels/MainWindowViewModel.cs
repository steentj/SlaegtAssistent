using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGedcomLoader _gedcomLoader;
    private readonly IGedcomFilePickerService _gedcomFilePickerService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly ISettingsDialogService _settingsDialogService;
    private readonly IUserDialogService _userDialogService;
    private readonly IApplicationControlService _applicationControlService;
    private readonly IMarkdownBiographyExportService _markdownBiographyExportService;
    private readonly IMarkdownFileStore _markdownFileStore;

    public MainWindowViewModel()
        : this(
            new GedcomLoader(),
            new NullGedcomFilePickerService(),
            new NullFolderPickerService(),
            new NullApplicationSettingsService(),
            new NullSettingsDialogService(),
            new NullUserDialogService(),
            new NullApplicationControlService(),
            new NullMarkdownBiographyExportService(),
            new NullMarkdownFileStore())
    {
    }

    public MainWindowViewModel(
        IGedcomLoader gedcomLoader,
        IGedcomFilePickerService gedcomFilePickerService,
        IFolderPickerService folderPickerService,
        IApplicationSettingsService applicationSettingsService,
        ISettingsDialogService settingsDialogService,
        IUserDialogService userDialogService,
        IApplicationControlService applicationControlService,
        IMarkdownBiographyExportService markdownBiographyExportService,
        IMarkdownFileStore markdownFileStore)
    {
        _gedcomLoader = gedcomLoader;
        _gedcomFilePickerService = gedcomFilePickerService;
        _folderPickerService = folderPickerService;
        _applicationSettingsService = applicationSettingsService;
        _settingsDialogService = settingsDialogService;
        _userDialogService = userDialogService;
        _applicationControlService = applicationControlService;
        _markdownBiographyExportService = markdownBiographyExportService;
        _markdownFileStore = markdownFileStore;

        var settings = _applicationSettingsService.Load();
        StandardGedcomInputFolder = NormalizeFolder(settings.DefaultGedcomInputFolder);
        StandardMarkdownOutputFolder = NormalizeFolder(settings.DefaultMarkdownOutputFolder);
    }

    [ObservableProperty]
    private PersonListItemViewModel? selectedPerson;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? selectedGedcomFilePath;

    [ObservableProperty]
    private string? standardGedcomInputFolder;

    [ObservableProperty]
    private string? standardMarkdownOutputFolder;

    [ObservableProperty]
    private EditorViewModel? editor;

    public ObservableCollection<PersonListItemViewModel> People { get; } = [];

    [RelayCommand]
    private async Task SelectGedcomFileAsync()
    {
        var filePath = await _gedcomFilePickerService.PickGedcomFileAsync(StandardGedcomInputFolder);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        SetDefaultInputFolderFromSelectedGedcom(filePath);

        if (!await EnsureOutputFolderAsync(filePath))
        {
            return;
        }

        try
        {
            var familyTree = _gedcomLoader.Load(filePath);
            _markdownBiographyExportService.WriteBiographies(familyTree, StandardMarkdownOutputFolder!);
            var outputFolder = StandardMarkdownOutputFolder!;

            var people = familyTree.People
                .Select(person => CreatePersonListItem(person, outputFolder))
                .OrderBy(person => person.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(person => person.RecordId, StringComparer.Ordinal)
                .ToList();

            ReplacePeople(people);
            SelectedPerson = People.FirstOrDefault();
            SelectedGedcomFilePath = filePath;
        }
        catch (GedcomLoadException exception)
        {
            ErrorMessage = $"Kunne ikke indlæse GEDCOM-fil: {exception.Message}";
        }
        catch (IOException exception)
        {
            ErrorMessage = $"Kunne ikke skrive Markdown-filer: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            ErrorMessage = $"Manglende adgang til outputmappe: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var updatedSettings = await _settingsDialogService.EditSettingsAsync(new AppSettings
        {
            DefaultGedcomInputFolder = StandardGedcomInputFolder,
            DefaultMarkdownOutputFolder = StandardMarkdownOutputFolder,
        });

        if (updatedSettings is null)
        {
            return;
        }

        StandardGedcomInputFolder = NormalizeFolder(updatedSettings.DefaultGedcomInputFolder);
        StandardMarkdownOutputFolder = NormalizeFolder(updatedSettings.DefaultMarkdownOutputFolder);
        SaveSettings();
    }

    [RelayCommand]
    private Task ShowIntroductionAsync()
    {
        return _userDialogService.ShowInformationAsync(
            "Generel introduktion",
            "Slægtsassistent hjælper dig med at indlæse GEDCOM-data, generere Markdown-biografier " +
            "og redigere indhold lokalt på din egen computer.");
    }

    [RelayCommand]
    private Task ShowAboutAsync()
    {
        return _userDialogService.ShowInformationAsync(
            "Om",
            "Slægtsassistent er et lokalt værktøj til slægtsforskning med fokus på privatliv og " +
            "manuel kvalitetssikring af biografier.");
    }

    [RelayCommand]
    private void ExitApplication()
    {
        _applicationControlService.Exit();
    }

    private async Task<bool> EnsureOutputFolderAsync(string gedcomFilePath)
    {
        if (!string.IsNullOrWhiteSpace(StandardMarkdownOutputFolder))
        {
            return true;
        }

        var gedcomFolder = NormalizeFolder(Path.GetDirectoryName(gedcomFilePath));
        var selectedOutputFolder = await _folderPickerService.PickFolderAsync(
            "Vælg standardmappe for Markdown-filer",
            gedcomFolder ?? StandardGedcomInputFolder);

        if (string.IsNullOrWhiteSpace(selectedOutputFolder))
        {
            ErrorMessage = "Du skal vælge en outputmappe til Markdown-filer, før GEDCOM-filen kan indlæses.";
            return false;
        }

        StandardMarkdownOutputFolder = selectedOutputFolder;
        SaveSettings();
        return true;
    }

    private void SetDefaultInputFolderFromSelectedGedcom(string gedcomFilePath)
    {
        if (!string.IsNullOrWhiteSpace(StandardGedcomInputFolder))
        {
            return;
        }

        var folder = NormalizeFolder(Path.GetDirectoryName(gedcomFilePath));
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        StandardGedcomInputFolder = folder;
        SaveSettings();
    }

    partial void OnSelectedPersonChanged(PersonListItemViewModel? value)
    {
        if (value is null)
        {
            Editor = null;
            return;
        }

        var editor = new EditorViewModel(value.MarkdownFilePath, _markdownFileStore);
        try
        {
            editor.Load();
            Editor = editor;
            ErrorMessage = null;
        }
        catch (IOException exception)
        {
            Editor = null;
            ErrorMessage = $"Kunne ikke læse Markdown-fil: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            Editor = null;
            ErrorMessage = $"Manglende adgang til Markdown-fil: {exception.Message}";
        }
    }

    private static PersonListItemViewModel CreatePersonListItem(Person person, string outputFolder)
    {
        var displayName = string.IsNullOrWhiteSpace(person.FullName)
            ? $"Unavngiven ({person.RecordId})"
            : person.FullName.Trim();
        var markdownFileName = BiographyFileNameGenerator.Generate(person);
        var markdownFilePath = Path.Combine(outputFolder, markdownFileName);

        return new PersonListItemViewModel(person.RecordId, displayName, markdownFilePath);
    }

    private void ReplacePeople(IEnumerable<PersonListItemViewModel> people)
    {
        People.Clear();
        foreach (var person in people)
        {
            People.Add(person);
        }
    }

    private void SaveSettings()
    {
        _applicationSettingsService.Save(new AppSettings
        {
            DefaultGedcomInputFolder = StandardGedcomInputFolder,
            DefaultMarkdownOutputFolder = StandardMarkdownOutputFolder,
        });
    }

    private static string? NormalizeFolder(string? folder)
    {
        return string.IsNullOrWhiteSpace(folder) ? null : folder.Trim();
    }

    private sealed class NullGedcomFilePickerService : IGedcomFilePickerService
    {
        public Task<string?> PickGedcomFileAsync(string? suggestedStartFolder)
        {
            return Task.FromResult<string?>(null);
        }
    }

    private sealed class NullFolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder)
        {
            return Task.FromResult<string?>(null);
        }
    }

    private sealed class NullApplicationSettingsService : IApplicationSettingsService
    {
        public AppSettings Load()
        {
            return new AppSettings();
        }

        public void Save(AppSettings settings)
        {
        }
    }

    private sealed class NullUserDialogService : IUserDialogService
    {
        public Task ShowInformationAsync(string title, string message)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NullSettingsDialogService : ISettingsDialogService
    {
        public Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings)
        {
            return Task.FromResult<AppSettings?>(null);
        }
    }

    private sealed class NullApplicationControlService : IApplicationControlService
    {
        public void Exit()
        {
        }
    }

    private sealed class NullMarkdownBiographyExportService : IMarkdownBiographyExportService
    {
        public void WriteBiographies(FamilyTree familyTree, string outputDirectory)
        {
        }
    }

    private sealed class NullMarkdownFileStore : IMarkdownFileStore
    {
        public string Read(string path)
        {
            return string.Empty;
        }

        public void Write(string path, string content)
        {
        }
    }
}
