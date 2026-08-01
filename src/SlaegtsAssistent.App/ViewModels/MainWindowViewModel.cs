using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ComponentModel;
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
    private readonly IUnsavedChangesDialogService _unsavedChangesDialogService;
    private readonly IApplicationControlService _applicationControlService;
    private readonly IMarkdownBiographyExportService _markdownBiographyExportService;
    private readonly IMarkdownFileStore _markdownFileStore;
    private readonly IMarkdownDocumentCatalog _markdownDocumentCatalog;
    private readonly IGedcomDifferenceDialogService _gedcomDifferenceDialogService;
    private readonly List<PersonListItemViewModel> _documentPeople = [];
    private readonly List<PersonListItemViewModel> _allPeople = [];
    private readonly Dictionary<string, EditorViewModel> _editors = new(StringComparer.Ordinal);

    public MainWindowViewModel()
        : this(
            new GedcomLoader(),
            new NullGedcomFilePickerService(),
            new NullFolderPickerService(),
            new NullApplicationSettingsService(),
            new NullSettingsDialogService(),
            new NullUserDialogService(),
            new NullUnsavedChangesDialogService(),
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
        IUnsavedChangesDialogService unsavedChangesDialogService,
        IApplicationControlService applicationControlService,
        IMarkdownBiographyExportService markdownBiographyExportService,
        IMarkdownFileStore markdownFileStore,
        IMarkdownDocumentCatalog? markdownDocumentCatalog = null,
        IGedcomDifferenceDialogService? gedcomDifferenceDialogService = null)
    {
        _gedcomLoader = gedcomLoader;
        _gedcomFilePickerService = gedcomFilePickerService;
        _folderPickerService = folderPickerService;
        _applicationSettingsService = applicationSettingsService;
        _settingsDialogService = settingsDialogService;
        _userDialogService = userDialogService;
        _unsavedChangesDialogService = unsavedChangesDialogService;
        _applicationControlService = applicationControlService;
        _markdownBiographyExportService = markdownBiographyExportService;
        _markdownFileStore = markdownFileStore;
        _markdownDocumentCatalog = markdownDocumentCatalog ?? new FileSystemMarkdownDocumentCatalog();
        _gedcomDifferenceDialogService = gedcomDifferenceDialogService ?? new NullGedcomDifferenceDialogService();

        var settings = _applicationSettingsService.Load();
        StandardGedcomInputFolder = NormalizeFolder(settings.DefaultGedcomInputFolder);
        StandardMarkdownOutputFolder = NormalizeFolder(settings.DefaultMarkdownOutputFolder);
        Theme = settings.Theme;
        _documentPeople.AddRange(_markdownDocumentCatalog.Load(StandardMarkdownOutputFolder)
            .Select(document => new PersonListItemViewModel(
                document.RecordId,
                document.DisplayName,
                document.FilePath,
                document.ErrorMessage ?? string.Empty)));
        ReplaceAllPeople(_documentPeople);
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
    private ThemePreference theme = ThemePreference.System;

    [ObservableProperty]
    private EditorViewModel? editor;

    [ObservableProperty]
    private string personFilterText = string.Empty;

    [ObservableProperty]
    private bool hasDirtyEditors;

    public ObservableCollection<PersonListItemViewModel> People { get; } = [];

    public string ActivePersonText => SelectedPerson is null
        ? "Ingen person valgt"
        : $"{SelectedPerson.DisplayName} ({SelectedPerson.RecordId})";

    public string ActiveFilePathText => AbbreviatePath(SelectedGedcomFilePath)
        ?? "Ingen GEDCOM-fil indlæst";

    public string ActiveMarkdownFilePathText => SelectedPerson is null
        ? "Ingen redigeringsfil"
        : AbbreviatePath(SelectedPerson.MarkdownFilePath) ?? "Ingen redigeringsfil";

    public string SaveStatusText => HasDirtyEditors ? "Ugemte ændringer" : "Gemt";

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
            await ReviewGedcomDifferencesAsync(familyTree);

            var people = familyTree.People
                .Select(person => CreatePersonListItem(person, outputFolder))
                .OrderBy(person => person.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(person => person.RecordId, StringComparer.Ordinal)
                .ToList();

            var gedcomRecordIds = people.Select(person => person.RecordId).ToHashSet(StringComparer.Ordinal);
            people.AddRange(_documentPeople.Where(person =>
                !gedcomRecordIds.Contains(person.RecordId)));

            ReplaceAllPeople(people);
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
            Theme = Theme,
        });

        if (updatedSettings is null)
        {
            return;
        }

        StandardGedcomInputFolder = NormalizeFolder(updatedSettings.DefaultGedcomInputFolder);
        StandardMarkdownOutputFolder = NormalizeFolder(updatedSettings.DefaultMarkdownOutputFolder);
        Theme = updatedSettings.Theme;
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

    public async Task<bool> ConfirmCloseAsync()
    {
        if (!HasDirtyEditors)
        {
            return true;
        }

        var decision = await _unsavedChangesDialogService.AskAsync();
        if (decision == UnsavedChangesDecision.Gem)
        {
            SaveAll();
            return true;
        }

        return decision == UnsavedChangesDecision.Kassér;
    }

    [RelayCommand(CanExecute = nameof(CanSaveAll))]
    private void SaveAll()
    {
        foreach (var editor in _editors.Values.Where(editor => editor.IsDirty).ToList())
        {
            editor.SaveCommand.Execute(null);
        }

        UpdateDirtyState();
    }

    [RelayCommand]
    private async Task ExitApplicationAsync()
    {
        if (!await ConfirmCloseAsync())
        {
            return;
        }

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

    private async Task ReviewGedcomDifferencesAsync(FamilyTree familyTree)
    {
        var reviewItems = new List<GedcomDifferenceReviewItem>();

        foreach (var person in familyTree.People)
        {
            var expectedPath = Path.Combine(
                StandardMarkdownOutputFolder!,
                BiographyFileNameGenerator.Generate(person));
            var matchedPerson = _documentPeople.FirstOrDefault(
                document => document.RecordId == person.RecordId)
                ?? _documentPeople.FirstOrDefault(
                    document => string.Equals(document.MarkdownFilePath, expectedPath, StringComparison.Ordinal));
            MarkdownDocumentInfo? documentInfo = matchedPerson is null
                ? null
                : new MarkdownDocumentInfo(
                    matchedPerson.RecordId,
                    matchedPerson.DisplayName,
                    matchedPerson.MarkdownFilePath,
                    matchedPerson.RawGedcom);
            if (documentInfo is null && File.Exists(expectedPath))
            {
                documentInfo = new MarkdownDocumentInfo(
                    $"legacy:{Path.GetFileName(expectedPath)}",
                    person.FullName ?? person.RecordId,
                    expectedPath);
            }

            if (documentInfo is null ||
                documentInfo.RecordId.StartsWith("error:", StringComparison.Ordinal))
            {
                continue;
            }

            var hasOpenEditor = _editors.TryGetValue(documentInfo.FilePath, out var openEditor);
            var document = hasOpenEditor
                ? openEditor!.CreateDocument()
                : BiographyDocumentParser.Parse(_markdownFileStore.Read(documentInfo.FilePath));

            var documentFacts = document.Metadata?.Facts
                ?? BiographyDocumentParser.ExtractVisibleFacts(
                    document.Body,
                        new BiographyFactsSnapshot(null, null, null, null, null, null, [])
                        {
                            RepresentedFields = new HashSet<string>(StringComparer.Ordinal),
                        });
            var reviewDocument = document.Metadata is not null
                ? document
                : document with
                {
                    Metadata = new BiographyDocumentMetadata(
                        1,
                        person.RecordId,
                        person.FullName,
                        documentFacts),
                };
            var gedcomFacts = BiographyFactsSnapshot.FromPerson(person);
            var differences = new BiographyDifferenceService().Compare(
                documentFacts,
                gedcomFacts);
            foreach (var difference in differences)
            {
                reviewItems.Add(new GedcomDifferenceReviewItem(
                    $"{documentInfo.FilePath}|{difference.FieldName}",
                    person.FullName ?? person.RecordId,
                    documentInfo.FilePath,
                    reviewDocument,
                    gedcomFacts,
                    difference));
            }
        }

        var choices = await _gedcomDifferenceDialogService.ShowAsync(reviewItems);
        if (choices is null)
        {
            return;
        }

        foreach (var documentGroup in reviewItems.GroupBy(item => item.FilePath, StringComparer.Ordinal))
        {
            var first = documentGroup.First();
            var selectedFields = documentGroup.ToDictionary(
                item => item.Difference.FieldName,
                item => choices.TryGetValue(item.Key, out var useGedcom) && useGedcom,
                StringComparer.Ordinal);
            if (!selectedFields.Values.Any(value => value))
            {
                continue;
            }

            var updatedContent = BiographyDocumentUpdater.ApplyGedcomChoices(
                first.Document,
                first.GedcomFacts,
                selectedFields);
            if (_editors.TryGetValue(first.FilePath, out var openEditor))
            {
                openEditor.ApplySerializedDocument(updatedContent);
            }
            else
            {
                _markdownFileStore.Write(first.FilePath, updatedContent);
            }
        }
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
        NotifyStatusProperties();

        if (value is null)
        {
            Editor = null;
            return;
        }

        if (!_editors.TryGetValue(value.MarkdownFilePath, out var editor))
        {
            editor = new EditorViewModel(value.MarkdownFilePath, _markdownFileStore);
            editor.PropertyChanged += EditorOnPropertyChanged;

            try
            {
                editor.Load();
                _editors.Add(value.MarkdownFilePath, editor);
            }
            catch (IOException exception)
            {
                editor.PropertyChanged -= EditorOnPropertyChanged;
                ErrorMessage = $"Kunne ikke læse Markdown-fil: {exception.Message}";
                Editor = null;
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                editor.PropertyChanged -= EditorOnPropertyChanged;
                ErrorMessage = $"Manglende adgang til Markdown-fil: {exception.Message}";
                Editor = null;
                return;
            }
        }

        Editor = editor;
        ErrorMessage = null;
        UpdateDirtyState();
    }

    private static PersonListItemViewModel CreatePersonListItem(Person person, string outputFolder)
    {
        var displayName = string.IsNullOrWhiteSpace(person.FullName)
            ? $"Unavngiven ({person.RecordId})"
            : person.FullName.Trim();
        var markdownFileName = BiographyFileNameGenerator.Generate(person);
        var markdownFilePath = Path.Combine(outputFolder, markdownFileName);

        return new PersonListItemViewModel(person.RecordId, displayName, markdownFilePath, person.RawGedcom);
    }

    partial void OnPersonFilterTextChanged(string value)
    {
        ApplyPersonFilter();
    }

    private void ReplaceAllPeople(IEnumerable<PersonListItemViewModel> people)
    {
        _allPeople.Clear();
        _allPeople.AddRange(people);
        ApplyPersonFilter();
    }

    private void ApplyPersonFilter()
    {
        var searchTerm = PersonFilterText?.Trim();
        var selectedRecordId = SelectedPerson?.RecordId;

        var filteredPeople = string.IsNullOrWhiteSpace(searchTerm)
            ? _allPeople
            : _allPeople.Where(person =>
                person.DisplayName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                person.RecordId.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase));

        ReplacePeople(filteredPeople);

        if (string.IsNullOrWhiteSpace(selectedRecordId) || !People.Any(person => person.RecordId == selectedRecordId))
        {
            SelectedPerson = People.FirstOrDefault();
        }
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
            Theme = Theme,
        });
    }

    private static string? NormalizeFolder(string? folder)
    {
        return string.IsNullOrWhiteSpace(folder) ? null : folder.Trim();
    }

    private static string? AbbreviatePath(string? path, int maximumLength = 72)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalizedPath = path.Trim();
        if (normalizedPath.Length <= maximumLength)
        {
            return normalizedPath;
        }

        var fileName = Path.GetFileName(normalizedPath);
        var prefixLength = maximumLength - fileName.Length - 3;
        return prefixLength > 0
            ? $"{normalizedPath[..prefixLength]}...{fileName}"
            : $"...{fileName}";
    }

    private void NotifyStatusProperties()
    {
        OnPropertyChanged(nameof(ActivePersonText));
        OnPropertyChanged(nameof(ActiveMarkdownFilePathText));
    }

    private bool CanSaveAll()
    {
        return HasDirtyEditors;
    }

    private void EditorOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(EditorViewModel.IsDirty))
        {
            UpdateDirtyState();
        }
    }

    private void UpdateDirtyState()
    {
        HasDirtyEditors = _editors.Values.Any(editor => editor.IsDirty);
        SaveAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedGedcomFilePathChanged(string? value)
    {
        OnPropertyChanged(nameof(ActiveFilePathText));
    }

    partial void OnHasDirtyEditorsChanged(bool value)
    {
        OnPropertyChanged(nameof(SaveStatusText));
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

    private sealed class NullUnsavedChangesDialogService : IUnsavedChangesDialogService
    {
        public Task<UnsavedChangesDecision> AskAsync()
        {
            return Task.FromResult(UnsavedChangesDecision.Annullér);
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

    private sealed class NullGedcomDifferenceDialogService : IGedcomDifferenceDialogService
    {
        public Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
            IReadOnlyList<GedcomDifferenceReviewItem> differences)
        {
            return Task.FromResult<IReadOnlyDictionary<string, bool>?>(null);
        }
    }
}
