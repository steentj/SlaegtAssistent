using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartWithEmptyPeopleAndNoSelection()
    {
        var viewModel = CreateViewModel();

        viewModel.People.Should().BeEmpty();
        viewModel.SelectedPerson.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldLoadSavedSettings()
    {
        var settings = new AppSettings
        {
            DefaultGedcomInputFolder = "/tmp/input",
            DefaultMarkdownOutputFolder = "/tmp/output",
        };
        var settingsService = new RecordingApplicationSettingsService(settings);
        var viewModel = CreateViewModel(settingsService: settingsService);

        viewModel.StandardGedcomInputFolder.Should().Be("/tmp/input");
        viewModel.StandardMarkdownOutputFolder.Should().Be("/tmp/output");
    }

    [Fact]
    public void SettingSelectedPerson_ShouldRaisePropertyChanged()
    {
        var viewModel = CreateViewModel();
        var selectedPerson = new PersonListItemViewModel("@I1@", "Anna Jensen", "/tmp/anna-jensen.md");
        var raisedPropertyNames = new List<string?>();

        viewModel.PropertyChanged += (_, args) => raisedPropertyNames.Add(args.PropertyName);

        viewModel.SelectedPerson = selectedPerson;

        raisedPropertyNames.Should().Contain(nameof(MainWindowViewModel.SelectedPerson));
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldDoNothing_WhenFileSelectionIsCancelled()
    {
        var picker = new FakeGedcomFilePickerService(null);
        var loader = new RecordingGedcomLoader(path => throw new InvalidOperationException(path));
        var viewModel = CreateViewModel(gedcomFilePickerService: picker, gedcomLoader: loader);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        loader.Calls.Should().Be(0);
        viewModel.People.Should().BeEmpty();
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldUseConfiguredInputFolder_AsSuggestedStartFolder()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");

        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultGedcomInputFolder = "/tmp/start-her",
            DefaultMarkdownOutputFolder = "/tmp/output",
        });
        var picker = new FakeGedcomFilePickerService(file.Path);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var exporter = new RecordingMarkdownBiographyExportService();
        var viewModel = CreateViewModel(
            settingsService: settingsService,
            gedcomFilePickerService: picker,
            gedcomLoader: loader,
            markdownBiographyExportService: exporter);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        picker.LastSuggestedStartFolder.Should().Be("/tmp/start-her");
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldSetInputFolder_FromSelectedGedcomFolder_WhenMissing()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var picker = new FakeGedcomFilePickerService(file.Path);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var exporter = new RecordingMarkdownBiographyExportService();
        var viewModel = CreateViewModel(
            settingsService: settingsService,
            gedcomFilePickerService: picker,
            gedcomLoader: loader,
            markdownBiographyExportService: exporter);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        viewModel.StandardGedcomInputFolder.Should().Be(Path.GetDirectoryName(file.Path));
        settingsService.SavedSettings.Should().NotBeNull();
        settingsService.SavedSettings!.DefaultGedcomInputFolder.Should().Be(Path.GetDirectoryName(file.Path));
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldRequireOutputFolderBeforeLoading()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");

        var picker = new FakeGedcomFilePickerService(file.Path);
        var folderPicker = new RecordingFolderPickerService(null);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: picker,
            folderPickerService: folderPicker,
            gedcomLoader: loader,
            settingsService: new RecordingApplicationSettingsService(new AppSettings()));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        loader.Calls.Should().Be(0);
        viewModel.ErrorMessage.Should().Be("Du skal vælge en outputmappe til Markdown-filer, før GEDCOM-filen kan indlæses.");
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldLoadPeople_WhenFileIsSelected()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I2@ INDI",
            "1 NAME Bo /Jensen/",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var picker = new FakeGedcomFilePickerService(file.Path);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var exporter = new RecordingMarkdownBiographyExportService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: picker,
            gedcomLoader: loader,
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownBiographyExportService: exporter);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        loader.Calls.Should().Be(1);
        loader.LastPath.Should().Be(file.Path);
        viewModel.People.Select(person => person.DisplayName)
            .Should()
            .ContainInOrder("Anna Jensen", "Bo Jensen");
        viewModel.SelectedPerson?.DisplayName.Should().Be("Anna Jensen");
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.SelectedGedcomFilePath.Should().Be(file.Path);
        exporter.Calls.Should().Be(1);
        exporter.LastOutputFolder.Should().Be(outputFolder);
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldLoadSelectedPersonsMarkdown_IntoEditor()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var picker = new FakeGedcomFilePickerService(file.Path);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var markdownFileStore = new RecordingMarkdownFileStore(_ => "# Redigeret biografi");
        var viewModel = CreateViewModel(
            gedcomFilePickerService: picker,
            gedcomLoader: loader,
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: markdownFileStore);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        viewModel.Editor.Should().NotBeNull();
        viewModel.Editor!.MarkdownText.Should().Be("# Redigeret biografi");
        markdownFileStore.LastReadPath.Should().NotBeNull();
        markdownFileStore.LastReadPath.Should().EndWith(".md");
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldAskForOutputFolderAndThenLoad_WhenOutputIsMissing()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");

        var selectedOutputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var picker = new FakeGedcomFilePickerService(file.Path);
        var folderPicker = new RecordingFolderPickerService(selectedOutputFolder);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var settingsService = new RecordingApplicationSettingsService(new AppSettings());
        var exporter = new RecordingMarkdownBiographyExportService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: picker,
            folderPickerService: folderPicker,
            gedcomLoader: loader,
            settingsService: settingsService,
            markdownBiographyExportService: exporter);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        loader.Calls.Should().Be(1);
        viewModel.StandardMarkdownOutputFolder.Should().Be(selectedOutputFolder);
        folderPicker.Calls.Should().Be(1);
        exporter.LastOutputFolder.Should().Be(selectedOutputFolder);
        settingsService.SavedSettings.Should().NotBeNull();
        settingsService.SavedSettings!.DefaultMarkdownOutputFolder.Should().Be(selectedOutputFolder);
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldReplacePeople_WhenLoadingAgain()
    {
        using var firstFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        using var secondFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I2@ INDI",
            "1 NAME Bo /Jensen/",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var picker = new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: picker,
            gedcomLoader: loader,
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        loader.Calls.Should().Be(2);
        viewModel.People.Should().HaveCount(1);
        viewModel.People[0].DisplayName.Should().Be("Bo Jensen");
        viewModel.SelectedGedcomFilePath.Should().Be(secondFile.Path);
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldSetErrorMessage_WhenLoaderFails()
    {
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var picker = new FakeGedcomFilePickerService("/tmp/invalid.ged");
        var loader = new ThrowingGedcomLoader(new GedcomLoadException("Filen kunne ikke laeses."));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: picker,
            gedcomLoader: loader,
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("Kunne ikke indlæse GEDCOM-fil: Filen kunne ikke laeses.");
        viewModel.People.Should().BeEmpty();
    }

    [Fact]
    public async Task OpenSettingsCommand_ShouldPersistUpdatedFolders()
    {
        var settingsDialog = new RecordingSettingsDialogService(new AppSettings
        {
            DefaultGedcomInputFolder = "/tmp/ged-input",
            DefaultMarkdownOutputFolder = "/tmp/markdown-output",
        });
        var settingsService = new RecordingApplicationSettingsService(new AppSettings());
        var viewModel = CreateViewModel(settingsDialogService: settingsDialog, settingsService: settingsService);

        await viewModel.OpenSettingsCommand.ExecuteAsync(null);

        settingsDialog.Calls.Should().Be(1);
        viewModel.StandardGedcomInputFolder.Should().Be("/tmp/ged-input");
        viewModel.StandardMarkdownOutputFolder.Should().Be("/tmp/markdown-output");
        settingsService.SavedSettings.Should().NotBeNull();
        settingsService.SavedSettings!.DefaultGedcomInputFolder.Should().Be("/tmp/ged-input");
        settingsService.SavedSettings!.DefaultMarkdownOutputFolder.Should().Be("/tmp/markdown-output");
    }

    [Fact]
    public async Task OpenSettingsCommand_ShouldKeepValues_WhenDialogIsCancelled()
    {
        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultGedcomInputFolder = "/tmp/old-input",
            DefaultMarkdownOutputFolder = "/tmp/old-output",
        });
        var settingsDialog = new RecordingSettingsDialogService(null);
        var viewModel = CreateViewModel(settingsDialogService: settingsDialog, settingsService: settingsService);

        await viewModel.OpenSettingsCommand.ExecuteAsync(null);

        viewModel.StandardGedcomInputFolder.Should().Be("/tmp/old-input");
        viewModel.StandardMarkdownOutputFolder.Should().Be("/tmp/old-output");
        settingsService.SavedSettings.Should().BeNull();
    }

    [Fact]
    public void ExitApplicationCommand_ShouldCallApplicationControlService()
    {
        var applicationControlService = new RecordingApplicationControlService();
        var viewModel = CreateViewModel(applicationControlService: applicationControlService);

        viewModel.ExitApplicationCommand.Execute(null);

        applicationControlService.Calls.Should().Be(1);
    }

    private static MainWindowViewModel CreateViewModel(
        IGedcomLoader? gedcomLoader = null,
        IGedcomFilePickerService? gedcomFilePickerService = null,
        IFolderPickerService? folderPickerService = null,
        IApplicationSettingsService? settingsService = null,
        ISettingsDialogService? settingsDialogService = null,
        IUserDialogService? userDialogService = null,
        IApplicationControlService? applicationControlService = null,
        IMarkdownBiographyExportService? markdownBiographyExportService = null,
        IMarkdownFileStore? markdownFileStore = null)
    {
        return new MainWindowViewModel(
            gedcomLoader ?? new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            gedcomFilePickerService ?? new FakeGedcomFilePickerService(null),
            folderPickerService ?? new RecordingFolderPickerService(null),
            settingsService ?? new RecordingApplicationSettingsService(new AppSettings()),
            settingsDialogService ?? new RecordingSettingsDialogService(null),
            userDialogService ?? new NullUserDialogService(),
            applicationControlService ?? new RecordingApplicationControlService(),
            markdownBiographyExportService ?? new RecordingMarkdownBiographyExportService(),
            markdownFileStore ?? new RecordingMarkdownFileStore());
    }

    private static TemporaryGedcomFile CreateTemporaryGedcomFile(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllLines(filePath, lines);
        return new TemporaryGedcomFile(filePath);
    }

    private sealed class FakeGedcomFilePickerService : IGedcomFilePickerService
    {
        private readonly string? _path;

        public FakeGedcomFilePickerService(string? path)
        {
            _path = path;
        }

        public string? LastSuggestedStartFolder { get; private set; }

        public Task<string?> PickGedcomFileAsync(string? suggestedStartFolder)
        {
            LastSuggestedStartFolder = suggestedStartFolder;
            return Task.FromResult(_path);
        }
    }

    private sealed class SequencedGedcomFilePickerService : IGedcomFilePickerService
    {
        private readonly Queue<string?> _paths;

        public SequencedGedcomFilePickerService(IEnumerable<string?> paths)
        {
            _paths = new Queue<string?>(paths);
        }

        public Task<string?> PickGedcomFileAsync(string? suggestedStartFolder)
        {
            return Task.FromResult(_paths.Dequeue());
        }
    }

    private sealed class RecordingFolderPickerService : IFolderPickerService
    {
        private readonly string? _folderToReturn;

        public RecordingFolderPickerService(string? folderToReturn)
        {
            _folderToReturn = folderToReturn;
        }

        public int Calls { get; private set; }

        public string? LastSuggestedStartFolder { get; private set; }

        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder)
        {
            Calls++;
            LastSuggestedStartFolder = suggestedStartFolder;
            return Task.FromResult(_folderToReturn);
        }
    }

    private sealed class RecordingApplicationSettingsService : IApplicationSettingsService
    {
        private readonly AppSettings _loadedSettings;

        public RecordingApplicationSettingsService(AppSettings loadedSettings)
        {
            _loadedSettings = loadedSettings;
        }

        public AppSettings? SavedSettings { get; private set; }

        public AppSettings Load()
        {
            return new AppSettings
            {
                DefaultGedcomInputFolder = _loadedSettings.DefaultGedcomInputFolder,
                DefaultMarkdownOutputFolder = _loadedSettings.DefaultMarkdownOutputFolder,
            };
        }

        public void Save(AppSettings settings)
        {
            SavedSettings = new AppSettings
            {
                DefaultGedcomInputFolder = settings.DefaultGedcomInputFolder,
                DefaultMarkdownOutputFolder = settings.DefaultMarkdownOutputFolder,
            };
        }
    }

    private sealed class RecordingSettingsDialogService : ISettingsDialogService
    {
        private readonly AppSettings? _result;

        public RecordingSettingsDialogService(AppSettings? result)
        {
            _result = result;
        }

        public int Calls { get; private set; }

        public Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings)
        {
            Calls++;
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingMarkdownBiographyExportService : IMarkdownBiographyExportService
    {
        public int Calls { get; private set; }

        public string? LastOutputFolder { get; private set; }

        public void WriteBiographies(FamilyTree familyTree, string outputDirectory)
        {
            Calls++;
            LastOutputFolder = outputDirectory;
        }
    }

    private sealed class RecordingApplicationControlService : IApplicationControlService
    {
        public int Calls { get; private set; }

        public void Exit()
        {
            Calls++;
        }
    }

    private sealed class RecordingMarkdownFileStore : IMarkdownFileStore
    {
        private readonly Func<string, string> _read;

        public RecordingMarkdownFileStore(Func<string, string>? read = null)
        {
            _read = read ?? (_ => string.Empty);
        }

        public string? LastReadPath { get; private set; }

        public string Read(string path)
        {
            LastReadPath = path;
            return _read(path);
        }

        public void Write(string path, string content)
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

    private sealed class RecordingGedcomLoader : IGedcomLoader
    {
        private readonly Func<string, FamilyTree> _load;

        public RecordingGedcomLoader(Func<string, FamilyTree> load)
        {
            _load = load;
        }

        public int Calls { get; private set; }

        public string? LastPath { get; private set; }

        public FamilyTree Load(string filePath, FamilyTree? existingTree = null)
        {
            Calls++;
            LastPath = filePath;
            return _load(filePath);
        }
    }

    private sealed class ThrowingGedcomLoader : IGedcomLoader
    {
        private readonly Exception _exception;

        public ThrowingGedcomLoader(Exception exception)
        {
            _exception = exception;
        }

        public FamilyTree Load(string filePath, FamilyTree? existingTree = null)
        {
            throw _exception;
        }
    }

    private sealed class TemporaryGedcomFile : IDisposable
    {
        public TemporaryGedcomFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
