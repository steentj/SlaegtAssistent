using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Threading;
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
    private readonly IGedcomSnapshotStore _gedcomSnapshotStore;
    private readonly IGedcomDifferenceDialogService _gedcomDifferenceDialogService;
    private readonly IMarkdownCheatSheetService _markdownCheatSheetService;
    private readonly ITemplateCheatSheetService _templateCheatSheetService;
    private readonly List<PersonListItemViewModel> _documentPeople = [];
    private readonly List<PersonListItemViewModel> _allPeople = [];
    private readonly Dictionary<string, EditorViewModel> _editors = new(StringComparer.Ordinal);
    private FamilyTree? _familyTree;
    private CancellationTokenSource? _importCancellation;

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
        IGedcomDifferenceDialogService? gedcomDifferenceDialogService = null,
        IMarkdownCheatSheetService? markdownCheatSheetService = null,
        ITemplateCheatSheetService? templateCheatSheetService = null,
        IGedcomSnapshotStore? gedcomSnapshotStore = null)
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
        _gedcomSnapshotStore = gedcomSnapshotStore ?? new FileSystemGedcomSnapshotStore();
        _gedcomDifferenceDialogService = gedcomDifferenceDialogService ?? new NullGedcomDifferenceDialogService();
        _markdownCheatSheetService = markdownCheatSheetService ?? new NullMarkdownCheatSheetService();
        _templateCheatSheetService = templateCheatSheetService ?? new NullTemplateCheatSheetService();

        var settings = _applicationSettingsService.Load();
        StandardGedcomInputFolder = NormalizeFolder(settings.DefaultGedcomInputFolder);
        StandardMarkdownOutputFolder = NormalizeFolder(settings.DefaultMarkdownOutputFolder);
        GlobalBiographyTemplatePath = NormalizePath(settings.GlobalBiographyTemplatePath);
        Theme = settings.Theme;
        GedcomSnapshot? snapshot = null;
        string? snapshotError = null;
        try
        {
            snapshot = _gedcomSnapshotStore.Load(StandardMarkdownOutputFolder);
            if (snapshot is not null)
            {
                SelectedGedcomFilePath = snapshot.SourcePath;
            }
        }
        catch (GedcomSnapshotException exception)
        {
            snapshotError = $"Kunne ikke indlæse GEDCOM-snapshot: {exception.Message}";
        }

        ReloadDocumentCatalog(snapshot: snapshot);
        ReplaceAllPeople(_documentPeople);
        ErrorMessage = snapshotError;
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
    private string? globalBiographyTemplatePath;

    [ObservableProperty]
    private ThemePreference theme = ThemePreference.System;

    [ObservableProperty]
    private EditorViewModel? editor;

    [ObservableProperty]
    private string personFilterText = string.Empty;

    [ObservableProperty]
    private bool hasDirtyEditors;

    [ObservableProperty]
    private bool isImporting;

    [ObservableProperty]
    private string importPhaseText = "Klar";

    [ObservableProperty]
    private int importProgressPercent;

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

    public bool HasImportStatus => ImportPhaseText != "Klar";

    [RelayCommand(CanExecute = nameof(CanStartImport))]
    private async Task SelectGedcomFileAsync()
    {
        if (IsImporting)
        {
            return;
        }

        IsImporting = true;
        ImportPhaseText = "Vælger GEDCOM-fil";
        ImportProgressPercent = 0;
        ErrorMessage = null;
        _importCancellation = new CancellationTokenSource();
        var cancellationToken = _importCancellation.Token;

        try
        {
            var filePath = await _gedcomFilePickerService.PickGedcomFileAsync(StandardGedcomInputFolder);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                ImportPhaseText = "Klar";
                return;
            }

            var outputFolder = await ResolveOutputFolderAsync(filePath);
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                ImportPhaseText = "Klar";
                return;
            }

            SetImportPhase("Forhåndskontrol", 15);
            var preflight = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tree = _gedcomLoader.Load(filePath, null, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var documents = _markdownDocumentCatalog.Load(outputFolder);
                var generatedBiographies = tree.People.ToDictionary(
                    person => person.RecordId,
                    person =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var content = _markdownBiographyExportService.GenerateBiography(
                            tree,
                            person,
                            outputFolder);
                        var generatedDocument = BiographyDocumentParser.Parse(content);
                        if (generatedDocument.Metadata is null)
                        {
                            throw new FormatException(
                                $"Den genererede kandidat for '{person.RecordId}' mangler dokumentmetadata.");
                        }

                        return content;
                    },
                    StringComparer.Ordinal);
                return new ImportPreflight(tree, documents, generatedBiographies);
            }, cancellationToken);

            SetImportPhase("Gennemgang", 45);
            var review = await ReviewGedcomDifferencesAsync(
                preflight.FamilyTree,
                preflight.Documents,
                preflight.GeneratedBiographies,
                outputFolder,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            SetImportPhase("Gennemførelse", 70);
            var workspaceState = await Task.Run(
                () => CaptureImportState(outputFolder),
                CancellationToken.None);
            try
            {
                await Task.Run(() =>
                {
                    _markdownBiographyExportService.WriteBiographies(preflight.FamilyTree, outputFolder);
                    foreach (var change in review.Changes.Where(change => change.Editor is null))
                    {
                        _markdownFileStore.Write(change.FilePath, change.Content);
                    }

                    _gedcomSnapshotStore.Save(outputFolder, filePath, preflight.FamilyTree);
                }, CancellationToken.None);
            }
            catch (Exception commitException)
            {
                try
                {
                    await Task.Run(
                        () => RestoreImportState(outputFolder, workspaceState),
                        CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    throw new ImportCommitException(
                        "Importen fejlede, og automatisk gendannelse kunne ikke fuldføres. " +
                        "Luk ikke appen, og kontrollér arbejdsområdets filer manuelt.",
                        new AggregateException(commitException, rollbackException));
                }

                throw new ImportCommitException(
                    "Importen fejlede under gennemførelsen, og arbejdsområdet blev rullet tilbage.",
                    commitException);
            }

            foreach (var change in review.Changes.Where(change => change.Editor is not null))
            {
                change.Editor!.ApplySerializedDocument(change.Content);
            }

            SetImportPhase("Publicering", 90);
            StandardMarkdownOutputFolder = outputFolder;
            _familyTree = preflight.FamilyTree;
            ReloadDocumentCatalog(preflight.FamilyTree);
            PublishImport(preflight.FamilyTree, review.SyncStatuses, outputFolder, filePath);
            SetDefaultInputFolderFromSelectedGedcom(filePath);
            SetImportPhase("Færdig", 100);
        }
        catch (OperationCanceledException)
        {
            ImportPhaseText = "Annulleret";
            ErrorMessage = "GEDCOM-importen blev annulleret. Arbejdsområdet er uændret.";
        }
        catch (GedcomLoadException exception)
        {
            ImportPhaseText = "Fejl";
            ErrorMessage = $"Kunne ikke indlæse GEDCOM-fil: {exception.Message}";
        }
        catch (FormatException exception)
        {
            ImportPhaseText = "Fejl";
            ErrorMessage = $"Forhåndskontrollen af importen fejlede: {exception.Message}";
        }
        catch (ImportCommitException exception)
        {
            ImportPhaseText = "Fejl";
            ErrorMessage = exception.Message;
        }
        catch (IOException exception)
        {
            ImportPhaseText = "Fejl";
            ErrorMessage = $"Kunne ikke forhåndskontrollere importen: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            ImportPhaseText = "Fejl";
            ErrorMessage = $"Manglende adgang under importen: {exception.Message}";
        }
        finally
        {
            _importCancellation?.Dispose();
            _importCancellation = null;
            IsImporting = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelImport))]
    private void CancelImport()
    {
        _importCancellation?.Cancel();
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var previewPerson = SelectedPerson is null
            ? null
            : _familyTree?.FindPerson(SelectedPerson.RecordId);
        var updatedSettings = await _settingsDialogService.EditSettingsAsync(new AppSettings
        {
            DefaultGedcomInputFolder = StandardGedcomInputFolder,
            DefaultMarkdownOutputFolder = StandardMarkdownOutputFolder,
            GlobalBiographyTemplatePath = GlobalBiographyTemplatePath,
            Theme = Theme,
        }, previewPerson);

        if (updatedSettings is null)
        {
            return;
        }

        var previousSettings = CreateCurrentSettings();
        var proposedSettings = new AppSettings
        {
            DefaultGedcomInputFolder = NormalizeFolder(updatedSettings.DefaultGedcomInputFolder),
            DefaultMarkdownOutputFolder = NormalizeFolder(updatedSettings.DefaultMarkdownOutputFolder),
            GlobalBiographyTemplatePath = NormalizePath(updatedSettings.GlobalBiographyTemplatePath),
            Theme = updatedSettings.Theme,
        };
        var changesWorkspace = !AreSameWorkspace(
            previousSettings.DefaultMarkdownOutputFolder,
            proposedSettings.DefaultMarkdownOutputFolder);
        if (changesWorkspace && !await ConfirmWorkspaceSwitchAsync())
        {
            return;
        }

        ApplySettings(proposedSettings);
        if (!SaveSettings())
        {
            ApplySettings(previousSettings);
            return;
        }

        if (changesWorkspace)
        {
            ActivateWorkspace();
        }
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
    private void ShowMarkdownCheatSheet()
    {
        _markdownCheatSheetService.Show();
    }

    [RelayCommand]
    private void ShowTemplateCheatSheet()
    {
        _templateCheatSheetService.Show();
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
            return TrySaveAll();
        }

        return decision == UnsavedChangesDecision.Kassér;
    }

    [RelayCommand(CanExecute = nameof(CanSaveAll))]
    private void SaveAll()
    {
        TrySaveAll();
    }

    private bool TrySaveAll()
    {
        try
        {
            foreach (var editor in _editors.Values.Where(editor => editor.IsDirty).ToList())
            {
                editor.SaveCommand.Execute(null);
            }

            UpdateDirtyState();
            ErrorMessage = null;
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            UpdateDirtyState();
            ErrorMessage = $"Manglende adgang til at gemme Markdown-fil: {exception.Message}";
            return false;
        }
        catch (IOException exception)
        {
            UpdateDirtyState();
            ErrorMessage = $"Kunne ikke gemme Markdown-fil: {exception.Message}";
            return false;
        }
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

    private async Task<string?> ResolveOutputFolderAsync(string gedcomFilePath)
    {
        if (!string.IsNullOrWhiteSpace(StandardMarkdownOutputFolder))
        {
            return StandardMarkdownOutputFolder;
        }

        var gedcomFolder = NormalizeFolder(Path.GetDirectoryName(gedcomFilePath));
        var selectedOutputFolder = await _folderPickerService.PickFolderAsync(
            "Vælg standardmappe for Markdown-filer",
            gedcomFolder ?? StandardGedcomInputFolder);

        if (string.IsNullOrWhiteSpace(selectedOutputFolder))
        {
            ErrorMessage = "Du skal vælge en outputmappe til Markdown-filer, før GEDCOM-filen kan indlæses.";
            return null;
        }

        return NormalizeFolder(selectedOutputFolder);
    }

    private async Task<ImportReviewResult> ReviewGedcomDifferencesAsync(
        FamilyTree familyTree,
        IReadOnlyList<MarkdownDocumentInfo> documents,
        IReadOnlyDictionary<string, string> generatedBiographies,
        string outputFolder,
        CancellationToken cancellationToken)
    {
        var reviewItems = new List<GedcomDifferenceReviewItem>();
        var syncStatuses = new Dictionary<string, BiographySyncStatus>(StringComparer.Ordinal);
        var changes = new List<PlannedDocumentChange>();

        foreach (var person in familyTree.People)
        {
            var expectedPath = Path.Combine(
                outputFolder,
                BiographyFileNameGenerator.Generate(person));
            var recordIdMatches = documents
                .Where(document => document.RecordId == person.RecordId)
                .ToList();
            if (recordIdMatches.Count > 1)
            {
                syncStatuses[person.RecordId] = BiographySyncStatus.Tvetydig;
                continue;
            }

            var pathDocument = documents.FirstOrDefault(
                document => string.Equals(document.FilePath, expectedPath, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(pathDocument?.ErrorCategory))
            {
                syncStatuses[person.RecordId] = BiographySyncStatus.Ukendt;
                continue;
            }

            var matchedPerson = recordIdMatches.SingleOrDefault()
                ?? documents.FirstOrDefault(
                    document => !document.RecordId.StartsWith("error:", StringComparison.Ordinal) &&
                                string.Equals(document.FilePath, expectedPath, StringComparison.Ordinal));
            MarkdownDocumentInfo? documentInfo = matchedPerson;
            if (documentInfo is null && pathDocument is null && File.Exists(expectedPath))
            {
                documentInfo = new MarkdownDocumentInfo(
                    $"legacy:{Path.GetFileName(expectedPath)}",
                    person.FullName ?? person.RecordId,
                    expectedPath);
            }

            if (documentInfo is null ||
                documentInfo.RecordId.StartsWith("error:", StringComparison.Ordinal))
            {
                syncStatuses[person.RecordId] = BiographySyncStatus.Ny;
                continue;
            }

            var hasOpenEditor = _editors.TryGetValue(documentInfo.FilePath, out var openEditor);
            var document = hasOpenEditor
                ? openEditor!.CreateDocument()
                : BiographyDocumentParser.Parse(_markdownFileStore.Read(documentInfo.FilePath));

            var generatedContent = generatedBiographies[person.RecordId];
            var generatedDocument = BiographyDocumentParser.Parse(generatedContent);
            if (generatedDocument.Metadata is null)
            {
                throw new FormatException(
                    "Den genererede kandidat mangler dokumentmetadata.");
            }

            var generatedSectionCandidate = BiographyGeneratedSectionMerger.CreateCandidate(
                document.Body,
                generatedDocument.Body);
            var candidateMetadata = generatedDocument.Metadata;
            var candidateContent = BiographyDocumentSerializer.Serialize(
                candidateMetadata,
                generatedSectionCandidate.Content);
            var metadataMatches = document.Metadata is not null &&
                                  string.Equals(
                                      document.Metadata.GedcomBaselineHash,
                                      candidateMetadata.GedcomBaselineHash,
                                      StringComparison.Ordinal) &&
                                  string.Equals(
                                      document.Metadata.TemplateHash,
                                      candidateMetadata.TemplateHash,
                                      StringComparison.Ordinal);
            if (metadataMatches && !generatedSectionCandidate.ChangesExistingDocument)
            {
                syncStatuses[person.RecordId] = BiographySyncStatus.Uændret;
                continue;
            }

            syncStatuses[person.RecordId] = BiographySyncStatus.Ændret;
            var reviewDocument = document.Metadata is not null
                ? document
                : generatedDocument;
            var candidateDocument = BiographyDocumentParser.Parse(candidateContent);
            var difference = new BiographyDifference(
                "Genereret sektion",
                document.Body,
                candidateDocument.Body);
            var reviewItem = new GedcomDifferenceReviewItem(
                $"{documentInfo.FilePath}|generated",
                person.FullName ?? person.RecordId,
                documentInfo.FilePath,
                reviewDocument,
                BiographyFactsSnapshot.FromPerson(person),
                difference,
                true)
            {
                CandidateContent = candidateContent,
                RequiresMigration = generatedSectionCandidate.RequiresMigration ||
                                    document.Metadata?.FormatVersion < BiographyDocumentParser.CurrentFormatVersion,
            };
            reviewItems.Add(reviewItem);
        }

        var choices = await _gedcomDifferenceDialogService.ShowAsync(reviewItems)
            .WaitAsync(cancellationToken);
        if (choices is null)
        {
            return new ImportReviewResult(syncStatuses, changes);
        }

        foreach (var documentGroup in reviewItems.GroupBy(item => item.FilePath, StringComparer.Ordinal))
        {
            var first = documentGroup.First();
            if (first.CandidateContent is { } candidateContent)
            {
                if (!choices.TryGetValue(first.Key, out var useCandidate) || !useCandidate)
                {
                    continue;
                }

                _editors.TryGetValue(first.FilePath, out var candidateEditor);
                changes.Add(new PlannedDocumentChange(
                    first.FilePath,
                    candidateContent,
                    candidateEditor));

                continue;
            }

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
            _editors.TryGetValue(first.FilePath, out var openEditor);
            changes.Add(new PlannedDocumentChange(first.FilePath, updatedContent, openEditor));
        }

        return new ImportReviewResult(syncStatuses, changes);
    }

    private void PublishImport(
        FamilyTree familyTree,
        IReadOnlyDictionary<string, BiographySyncStatus> syncStatuses,
        string outputFolder,
        string filePath)
    {
        var people = familyTree.People
            .Select(person =>
            {
                var syncStatus = syncStatuses.TryGetValue(person.RecordId, out var status)
                    ? status
                    : BiographySyncStatus.Ukendt;
                var documentMatches = _documentPeople
                    .Where(document => document.RecordId == person.RecordId)
                    .ToList();
                var expectedPath = Path.Combine(
                    outputFolder,
                    BiographyFileNameGenerator.Generate(person));
                var pathMatch = _documentPeople.FirstOrDefault(document => string.Equals(
                    document.MarkdownFilePath,
                    expectedPath,
                    StringComparison.Ordinal));
                var stableFilePath = documentMatches.Count switch
                {
                    1 => documentMatches[0].MarkdownFilePath,
                    > 1 => string.Empty,
                    _ => pathMatch?.MarkdownFilePath,
                };
                var duplicatePaths = documentMatches.Count > 1
                    ? string.Join(", ", documentMatches.Select(document => document.MarkdownFilePath))
                    : null;
                return CreatePersonListItem(
                    person,
                    outputFolder,
                    syncStatus,
                    stableFilePath,
                    documentMatches.Count > 1
                        ? "Tvetydigt record-id"
                        : pathMatch?.DocumentErrorCategory,
                    documentMatches.Count > 1
                        ? $"Record-id '{person.RecordId}' findes i flere dokumenter: {duplicatePaths}."
                        : pathMatch?.DocumentErrorMessage,
                    documentMatches.Count > 1
                        ? "Sammenlign filerne manuelt, og behold eller ret kun den tilsigtede fil."
                        : pathMatch?.DocumentNextAction);
            })
            .OrderBy(person => person.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(person => person.RecordId, StringComparer.Ordinal)
            .ToList();

        var gedcomRecordIds = people.Select(person => person.RecordId).ToHashSet(StringComparer.Ordinal);
        var representedPaths = people
            .Select(person => person.MarkdownFilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.Ordinal);
        people.AddRange(_documentPeople.Where(person =>
            !gedcomRecordIds.Contains(person.RecordId) &&
            !representedPaths.Contains(person.MarkdownFilePath)));

        ReplaceAllPeople(people);
        SelectedPerson = People.FirstOrDefault();
        SelectedGedcomFilePath = filePath;
    }

    private static IReadOnlyDictionary<string, byte[]> CaptureImportState(string outputFolder)
    {
        if (!Directory.Exists(outputFolder))
        {
            return new Dictionary<string, byte[]>(StringComparer.Ordinal);
        }

        var paths = Directory.EnumerateFiles(outputFolder, "*.md", SearchOption.TopDirectoryOnly)
            .ToList();
        var internalDirectory = Path.Combine(outputFolder, ".slaegtsassistent");
        if (Directory.Exists(internalDirectory))
        {
            paths.AddRange(Directory.EnumerateFiles(
                internalDirectory,
                "*",
                SearchOption.AllDirectories));
        }

        return paths
            .ToDictionary(Path.GetFullPath, File.ReadAllBytes, StringComparer.Ordinal);
    }

    private static void RestoreImportState(
        string outputFolder,
        IReadOnlyDictionary<string, byte[]> originalFiles)
    {
        Directory.CreateDirectory(outputFolder);
        var currentPaths = Directory.EnumerateFiles(
                outputFolder,
                "*.md",
                SearchOption.TopDirectoryOnly)
            .ToList();
        var internalDirectory = Path.Combine(outputFolder, ".slaegtsassistent");
        if (Directory.Exists(internalDirectory))
        {
            currentPaths.AddRange(Directory.EnumerateFiles(
                internalDirectory,
                "*",
                SearchOption.AllDirectories));
        }

        foreach (var currentPath in currentPaths)
        {
            if (!originalFiles.ContainsKey(Path.GetFullPath(currentPath)))
            {
                File.Delete(currentPath);
            }
        }

        var writer = new AtomicFileWriter();
        foreach (var originalFile in originalFiles)
        {
            if (File.Exists(originalFile.Key) &&
                File.ReadAllBytes(originalFile.Key).AsSpan().SequenceEqual(originalFile.Value))
            {
                continue;
            }

            writer.WriteBytes(originalFile.Key, originalFile.Value);
        }

        if (Directory.Exists(internalDirectory) &&
            !Directory.EnumerateFiles(internalDirectory, "*", SearchOption.AllDirectories).Any())
        {
            Directory.Delete(internalDirectory, recursive: true);
        }
    }

    private void SetImportPhase(string phase, int progressPercent)
    {
        ImportPhaseText = phase;
        ImportProgressPercent = progressPercent;
    }

    private bool CanStartImport() => !IsImporting;

    private bool CanCancelImport()
    {
        return IsImporting && ImportPhaseText is not "Gennemførelse" and not "Publicering";
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

        if (value.SyncStatus == BiographySyncStatus.Tvetydig)
        {
            Editor = null;
            ErrorMessage =
                $"Record-id '{value.RecordId}' findes i flere Markdown-dokumenter. " +
                "Ingen fil er valgt eller ændret automatisk.";
            return;
        }

        if (value.HasDocumentDiagnostic)
        {
            Editor = null;
            ErrorMessage = value.DocumentDiagnosticText;
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

    private static PersonListItemViewModel CreatePersonListItem(
        Person person,
        string outputFolder,
        BiographySyncStatus syncStatus = BiographySyncStatus.Ukendt,
        string? stableFilePath = null,
        string? documentErrorCategory = null,
        string? documentErrorMessage = null,
        string? documentNextAction = null)
    {
        var displayName = string.IsNullOrWhiteSpace(person.FullName)
            ? $"Unavngiven ({person.RecordId})"
            : person.FullName.Trim();
        var markdownFileName = BiographyFileNameGenerator.Generate(person);
        var markdownFilePath = stableFilePath ?? Path.Combine(outputFolder, markdownFileName);

        return new PersonListItemViewModel(
            person.RecordId,
            displayName,
            markdownFilePath,
            person.RawGedcom,
            syncStatus,
            documentErrorCategory,
            documentErrorMessage,
            documentNextAction);
    }

    private void ReloadDocumentCatalog(
        FamilyTree? familyTree = null,
        GedcomSnapshot? snapshot = null)
    {
        _documentPeople.Clear();
        _documentPeople.AddRange(_markdownDocumentCatalog.Load(StandardMarkdownOutputFolder)
            .Select(document => new PersonListItemViewModel(
                document.RecordId,
                document.DisplayName,
                document.FilePath,
                familyTree?.FindPerson(document.RecordId)?.RawGedcom
                    ?? (snapshot?.RawPersonSegments.TryGetValue(document.RecordId, out var rawGedcom) == true
                        ? rawGedcom
                        : string.Empty),
                BiographySyncStatus.Ukendt,
                document.ErrorCategory,
                document.ErrorMessage,
                document.NextAction,
                document.RequiresMigration)));
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

    private bool SaveSettings()
    {
        try
        {
            _applicationSettingsService.Save(new AppSettings
            {
                DefaultGedcomInputFolder = StandardGedcomInputFolder,
                DefaultMarkdownOutputFolder = StandardMarkdownOutputFolder,
                GlobalBiographyTemplatePath = GlobalBiographyTemplatePath,
                Theme = Theme,
            });
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            ErrorMessage = $"Kunne ikke gemme indstillinger på grund af manglende adgang: {exception.Message}";
            return false;
        }
        catch (IOException exception)
        {
            ErrorMessage = $"Kunne ikke gemme indstillinger: {exception.Message}";
            return false;
        }
    }

    private async Task<bool> ConfirmWorkspaceSwitchAsync()
    {
        if (!HasDirtyEditors)
        {
            return true;
        }

        var decision = await _unsavedChangesDialogService.AskAsync();
        return decision switch
        {
            UnsavedChangesDecision.Gem => TrySaveAll(),
            UnsavedChangesDecision.Kassér => true,
            _ => false,
        };
    }

    private void ActivateWorkspace()
    {
        SelectedPerson = null;
        foreach (var editor in _editors.Values)
        {
            editor.PropertyChanged -= EditorOnPropertyChanged;
        }

        _editors.Clear();
        Editor = null;
        _familyTree = null;
        SelectedGedcomFilePath = null;
        UpdateDirtyState();

        GedcomSnapshot? snapshot = null;
        string? snapshotError = null;
        try
        {
            snapshot = _gedcomSnapshotStore.Load(StandardMarkdownOutputFolder);
            SelectedGedcomFilePath = snapshot?.SourcePath;
        }
        catch (GedcomSnapshotException exception)
        {
            snapshotError = $"Kunne ikke indlæse GEDCOM-snapshot: {exception.Message}";
        }

        ReloadDocumentCatalog(snapshot: snapshot);
        ReplaceAllPeople(_documentPeople);
        SelectedPerson = People.FirstOrDefault();
        ErrorMessage = snapshotError;
    }

    private AppSettings CreateCurrentSettings()
    {
        return new AppSettings
        {
            DefaultGedcomInputFolder = StandardGedcomInputFolder,
            DefaultMarkdownOutputFolder = StandardMarkdownOutputFolder,
            GlobalBiographyTemplatePath = GlobalBiographyTemplatePath,
            Theme = Theme,
        };
    }

    private void ApplySettings(AppSettings settings)
    {
        StandardGedcomInputFolder = settings.DefaultGedcomInputFolder;
        StandardMarkdownOutputFolder = settings.DefaultMarkdownOutputFolder;
        GlobalBiographyTemplatePath = settings.GlobalBiographyTemplatePath;
        Theme = settings.Theme;
    }

    private static bool AreSameWorkspace(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
        {
            return string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(second);
        }

        try
        {
            var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), comparison);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return string.Equals(first, second, StringComparison.Ordinal);
        }
    }

    private static string? NormalizeFolder(string? folder)
    {
        return string.IsNullOrWhiteSpace(folder) ? null : folder.Trim();
    }

    private static string? NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
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

    partial void OnIsImportingChanged(bool value)
    {
        SelectGedcomFileCommand.NotifyCanExecuteChanged();
        CancelImportCommand.NotifyCanExecuteChanged();
    }

    partial void OnImportPhaseTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasImportStatus));
        CancelImportCommand.NotifyCanExecuteChanged();
    }

    private sealed record ImportPreflight(
        FamilyTree FamilyTree,
        IReadOnlyList<MarkdownDocumentInfo> Documents,
        IReadOnlyDictionary<string, string> GeneratedBiographies);

    private sealed record PlannedDocumentChange(
        string FilePath,
        string Content,
        EditorViewModel? Editor);

    private sealed record ImportReviewResult(
        IReadOnlyDictionary<string, BiographySyncStatus> SyncStatuses,
        IReadOnlyList<PlannedDocumentChange> Changes);

    private sealed class ImportCommitException(string message, Exception innerException)
        : IOException(message, innerException);

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

        public string GenerateBiography(
            FamilyTree familyTree,
            Person person,
            string outputDirectory)
        {
            return string.Empty;
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
