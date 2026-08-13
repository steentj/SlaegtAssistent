using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartWithEmptyPeopleAndNoSelection()
    {
        var viewModel = CreateViewModel();

        viewModel.People.Should().BeEmpty();
        viewModel.SelectedPerson.Should().BeNull();
        viewModel.ActivePersonText.Should().Be("Ingen person valgt");
        viewModel.ActiveFilePathText.Should().Be("Ingen GEDCOM-fil indlæst");
        viewModel.SaveStatusText.Should().Be("Gemt");
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
    public void Constructor_ShouldLoadRawGedcomFromPersistedSnapshot()
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
        var familyTree = new GedcomLoader().Load(file.Path);
        var person = familyTree.People.Single();
        Directory.CreateDirectory(outputFolder);
        File.WriteAllText(
            Path.Combine(outputFolder, BiographyFileNameGenerator.Generate(person)),
            new BiographyTemplateMarkdownGenerator().Generate(person));

        var snapshotStore = new FileSystemGedcomSnapshotStore();
        snapshotStore.Save(outputFolder, file.Path, familyTree);
        var viewModel = CreateViewModel(
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomSnapshotStore: snapshotStore);

        viewModel.SelectedGedcomFilePath.Should().Be(Path.GetFullPath(file.Path));
        viewModel.People.Should().ContainSingle();
        viewModel.People[0].RawGedcom.Should().Contain("0 @I1@ INDI");
        viewModel.People[0].RawGedcom.Should().Contain("1 NAME Anna /Jensen/");
    }

    [Fact]
    public void Constructor_ShouldExposeSnapshotError_WhenSnapshotIsCorrupt()
    {
        var viewModel = CreateViewModel(
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = "/tmp/output",
            }),
            gedcomSnapshotStore: new ThrowingGedcomSnapshotStore());

        viewModel.ErrorMessage.Should().Be("Kunne ikke indlæse GEDCOM-snapshot: Snapshot er ugyldigt.");
    }

    [Fact]
    public void Constructor_WhenCatalogContainsValidAndDefectiveFiles_ShouldExposeBothWithoutThrowing()
    {
        var folder = CreateBiographyWorkspace("@I1@", "Anna Jensen", "anna.md");
        var defectivePath = Path.Combine(folder, "defekt.md");
        File.WriteAllText(
            defectivePath,
            "---\nformatVersion: 99\nrecordId: \"@I2@\"\n---\n# Defekt\n");

        try
        {
            var viewModel = CreateViewModel(
                settingsService: new RecordingApplicationSettingsService(new AppSettings
                {
                    DefaultMarkdownOutputFolder = folder,
                }),
                markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
                markdownFileStore: new FileSystemMarkdownFileStore());

            viewModel.People.Should().HaveCount(2);
            viewModel.People.Should().Contain(person => person.RecordId == "@I1@");
            var defective = viewModel.People.Single(person => person.MarkdownFilePath == defectivePath);

            var action = () => viewModel.SelectedPerson = defective;

            action.Should().NotThrow();
            viewModel.Editor.Should().BeNull();
            viewModel.ErrorMessage.Should().Contain("Ikke-understøttet formatversion");
            viewModel.ErrorMessage.Should().Contain(defectivePath);
            viewModel.ErrorMessage.Should().Contain("Næste handling");
            File.ReadAllText(defectivePath).Should().Contain("formatVersion: 99");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
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
    public async Task SelectGedcomFileCommand_ShouldExposeRawGedcomAndStatusData()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 SEX F",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        viewModel.People.Should().ContainSingle();
        viewModel.People[0].RawGedcom.Should().Contain("0 @I1@ INDI");
        viewModel.People[0].RawGedcom.Should().Contain("1 NAME Anna /Jensen/");
        File.Exists(Path.Combine(
                outputFolder,
                ".slaegtsassistent",
                "gedcom",
                "manifest.json"))
            .Should()
            .BeTrue();
        viewModel.People[0].SyncStatus.Should().Be(BiographySyncStatus.Ny);
        viewModel.People[0].SyncStatusText.Should().Be("Ny");
        viewModel.ActivePersonText.Should().Be("Anna Jensen (@I1@)");
        viewModel.ActiveFilePathText.Should().Contain(Path.GetFileName(file.Path));
        viewModel.SaveStatusText.Should().Be("Gemt");

        viewModel.Editor!.MarkdownText = "# Ændret";

        viewModel.SaveStatusText.Should().Be("Ugemte ændringer");
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldNotReviewNewlyCreatedDocumentsAsChanges()
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

        var differenceDialog = new RecordingGedcomDifferenceDialogService();
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            gedcomDifferenceDialogService: differenceDialog);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        differenceDialog.Calls.Should().Be(1);
        differenceDialog.LastDifferences.Should().BeEmpty();
        viewModel.People.Should().ContainSingle(person =>
            person.SyncStatus == BiographySyncStatus.Ny);
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldTreatSameGedcomAsUnchanged()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 SEX F",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settings = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var differenceDialog = new RecordingGedcomDifferenceDialogService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: settings,
            gedcomDifferenceDialogService: differenceDialog,
            markdownBiographyExportService: new MarkdownBiographyExportService(settings),
            markdownFileStore: new FileSystemMarkdownFileStore());

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        var filesAfterFirstImport = SnapshotFiles(outputFolder);
        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        differenceDialog.LastDifferences.Should().BeEmpty();
        viewModel.People.Should().ContainSingle(person =>
            person.SyncStatus == BiographySyncStatus.Uændret);
        viewModel.ImportPhaseText.Should().Be("Færdig – ingen ændringer");
        SnapshotFiles(outputFolder).Should().BeEquivalentTo(filesAfterFirstImport);
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldDetectChangeInFieldHiddenByTemplate()
    {
        using var firstFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 NOTE Første note",
            "0 TRLR");
        using var secondFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 NOTE Ændret note, som standardskabelonen ikke viser",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settings = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var dialog = new RecordingGedcomDifferenceDialogService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]),
            settingsService: settings,
            gedcomDifferenceDialogService: dialog,
            markdownBiographyExportService: new MarkdownBiographyExportService(settings),
            markdownFileStore: new FileSystemMarkdownFileStore());

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            var before = File.ReadAllText(viewModel.People.Single().MarkdownFilePath);
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            dialog.LastDifferences.Should().ContainSingle();
            dialog.LastDifferences[0].Difference.FieldName.Should().Be("Synkroniseringsbaseline");
            dialog.LastDifferences[0].BaselineStatus.Should().Be(BiographyBaselineStatus.Changed);
            var reconciliation = dialog.LastDifferences[0].ReconciliationState;
            reconciliation.Should().NotBeNull();
            reconciliation!.Approved.Should().NotBeNull();
            reconciliation.Imported.Person.Notes
                .Should().ContainSingle("Ændret note, som standardskabelonen ikke viser");
            reconciliation.DocumentFacts.FullName.Should().Be("Anna Jensen");
            dialog.LastDifferences[0].Difference.DocumentValue.Should().NotBe(
                dialog.LastDifferences[0].Difference.GedcomValue);
            File.ReadAllText(viewModel.People.Single().MarkdownFilePath).Should().Be(before);
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldDefaultNewGedcomInformationToGedcom()
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
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 BIRT",
            "2 DATE 12 MAR 1900",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settings = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var differenceDialog = new RecordingGedcomDifferenceDialogService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]),
            settingsService: settings,
            gedcomDifferenceDialogService: differenceDialog,
            markdownBiographyExportService: new MarkdownBiographyExportService(settings),
            markdownFileStore: new FileSystemMarkdownFileStore());

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        differenceDialog.LastDifferences.Should().Contain(difference =>
            difference.Difference.FieldName == "Genereret sektion" &&
            difference.UseGedcomByDefault &&
            difference.CandidateContent!.Contains("12 MAR 1900", StringComparison.Ordinal));
        viewModel.ImportPhaseText.Should().Be("Afvist");
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldApplyApprovedCandidateAndPreserveFreeText()
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
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 BIRT",
            "2 DATE 12 MAR 1900",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settings = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var differenceDialog = new ChoosingGedcomDifferenceDialogService(true);
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]),
            settingsService: settings,
            gedcomDifferenceDialogService: differenceDialog,
            markdownBiographyExportService: new MarkdownBiographyExportService(settings),
            markdownFileStore: new FileSystemMarkdownFileStore());

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.Editor!.MarkdownText = viewModel.Editor.MarkdownText
            .Replace("_Skriv den fulde livshistorie her._", "Min egen tekst.", StringComparison.Ordinal);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        viewModel.Editor.MarkdownText.Should().Contain("Min egen tekst.");
        viewModel.Editor.MarkdownText.Should().Contain("12 MAR 1900");
        viewModel.Editor.IsDirty.Should().BeTrue();
        differenceDialog.LastDifferences.Should().NotBeEmpty();
        differenceDialog.LastDifferences.Should().OnlyContain(item =>
            item.CandidateContent != null && item.UseGedcomByDefault);
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldApplyIndividualFieldChoicesToPreviewCandidate()
    {
        using var firstFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 BIRT",
            "2 DATE 1 JAN 1900",
            "2 PLAC Odense",
            "0 TRLR");
        using var secondFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 BIRT",
            "2 DATE 2 FEB 1901",
            "2 PLAC Aarhus",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settings = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var dialog = new PathChoosingGedcomDifferenceDialogService("person.birthPlace");
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]),
            settingsService: settings,
            gedcomDifferenceDialogService: dialog,
            markdownBiographyExportService: new MarkdownBiographyExportService(settings),
            markdownFileStore: new FileSystemMarkdownFileStore());

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            var visibleFact = "1 JAN 1900 i Odense";
            var factIndex = viewModel.Editor!.MarkdownText.IndexOf(visibleFact, StringComparison.Ordinal);
            viewModel.Editor.MarkdownText = viewModel.Editor.MarkdownText[..factIndex] +
                                            "3 MAR 1902 i Odense" +
                                            viewModel.Editor.MarkdownText[(factIndex + visibleFact.Length)..];
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            dialog.LastDifferences.Select(item => item.StructuredDifference!.Path)
                .Should().Contain(["person.birthDate", "person.birthPlace"]);
            dialog.LastDifferences.Should().Contain(item =>
                item.StructuredDifference!.Path.StartsWith("person.events[", StringComparison.Ordinal));
            viewModel.Editor!.MarkdownText.Should().Contain("3 MAR 1902 i Aarhus");
            viewModel.Editor.MarkdownText.Should().Contain("1 JAN 1900");
            viewModel.Editor.MarkdownText.Should().NotContain("2 FEB 1901");
            viewModel.Editor.IsDirty.Should().BeTrue();
            var candidate = viewModel.Editor.CreateDocument();
            candidate.Metadata!.SyncBaseline!.Imported.Person.BirthPlace.Should().Be("Aarhus");
            candidate.Metadata.SyncBaseline.Approved.Person.BirthDate.Should().Be("3 MAR 1902");
            candidate.Metadata.SyncBaseline.Approved.Person.BirthPlace.Should().Be("Aarhus");
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldLeaveDocumentUnchanged_WhenCandidateIsRejected()
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
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 BIRT",
            "2 DATE 12 MAR 1900",
            "0 TRLR");

        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settings = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var differenceDialog = new ChoosingGedcomDifferenceDialogService(false);
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]),
            settingsService: settings,
            gedcomDifferenceDialogService: differenceDialog,
            markdownBiographyExportService: new MarkdownBiographyExportService(settings),
            markdownFileStore: new FileSystemMarkdownFileStore());

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        var original = viewModel.Editor!.MarkdownText;
        var snapshotDirectory = Path.Combine(outputFolder, ".slaegtsassistent", "gedcom");
        var snapshotBefore = Directory.GetFiles(snapshotDirectory)
            .ToDictionary(path => Path.GetFileName(path)!, File.ReadAllBytes, StringComparer.Ordinal);
        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        viewModel.Editor.MarkdownText.Should().Be(original);
        viewModel.Editor.MarkdownText.Should().NotContain("12 MAR 1900");
        viewModel.Editor.IsDirty.Should().BeFalse();
        viewModel.ImportPhaseText.Should().Be("Afvist");
        var snapshotAfter = Directory.GetFiles(snapshotDirectory)
            .ToDictionary(path => Path.GetFileName(path)!, File.ReadAllBytes, StringComparer.Ordinal);
        snapshotAfter.Keys.Should().BeEquivalentTo(snapshotBefore.Keys);
        foreach (var file in snapshotBefore)
        {
            snapshotAfter[file.Key].Should().Equal(file.Value);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldLoadPeople_WhenSelectedFileHasNoExtension()
    {
        using var file = CreateTemporaryGedcomFileWithoutExtension(
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
        viewModel.People.Should().HaveCount(1);
        viewModel.People[0].DisplayName.Should().Be("Anna Jensen");
        viewModel.SelectedGedcomFilePath.Should().Be(file.Path);
    }

    [Fact]
    public async Task PersonFilterText_ShouldFilterPeopleByName()
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
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.PersonFilterText = "anna";

        viewModel.People.Should().HaveCount(1);
        viewModel.People[0].DisplayName.Should().Be("Anna Jensen");
    }

    [Fact]
    public async Task PersonFilterText_ShouldFilterCaseInsensitive()
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
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.PersonFilterText = "ANNA";

        viewModel.People.Should().HaveCount(1);
        viewModel.People[0].DisplayName.Should().Be("Anna Jensen");
    }

    [Fact]
    public async Task PersonFilterText_ShouldShowAllPeople_WhenFilterIsEmpty()
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
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.PersonFilterText = "anna";
        viewModel.PersonFilterText = string.Empty;

        viewModel.People.Select(person => person.DisplayName)
            .Should()
            .ContainInOrder("Anna Jensen", "Bo Jensen");
    }

    [Fact]
    public async Task PersonFilterText_ShouldFilterPeopleByRecordId()
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
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.PersonFilterText = "i2";

        viewModel.People.Should().HaveCount(1);
        viewModel.People[0].RecordId.Should().Be("@I2@");
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
        viewModel.Editor.PreviewMode.Should().Be(PreviewMode.Web);
        markdownFileStore.LastReadPath.Should().NotBeNull();
        markdownFileStore.LastReadPath.Should().EndWith(".md");
    }

    [Fact]
    public async Task SaveAllCommand_ShouldSaveAllDirtyEditors()
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
        var markdownFileStore = new RecordingMarkdownFileStore(_ => "# Original tekst");
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: markdownFileStore);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        var anna = viewModel.People.Single(person => person.DisplayName == "Anna Jensen");
        var bo = viewModel.People.Single(person => person.DisplayName == "Bo Jensen");

        viewModel.SelectedPerson = anna;
        viewModel.Editor!.MarkdownText = "# Anna ændret";
        viewModel.SelectedPerson = bo;
        viewModel.Editor!.MarkdownText = "# Bo ændret";

        viewModel.HasDirtyEditors.Should().BeTrue();
        viewModel.SaveAllCommand.CanExecute(null).Should().BeTrue();

        viewModel.SaveAllCommand.Execute(null);

        markdownFileStore.Writes.Should().Contain(write =>
            write.Path == anna.MarkdownFilePath && write.Content == "# Anna ændret");
        markdownFileStore.Writes.Should().Contain(write =>
            write.Path == bo.MarkdownFilePath && write.Content == "# Bo ændret");
        viewModel.HasDirtyEditors.Should().BeFalse();
        viewModel.SaveAllCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task SaveAllCommand_WhenStorageFails_ShouldKeepChangesAndExposeDanishError()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: new FailingMarkdownFileStore());

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.Editor!.MarkdownText = "# Brugerens ændring";

        var action = () => viewModel.SaveAllCommand.Execute(null);

        action.Should().NotThrow();
        viewModel.Editor.IsDirty.Should().BeTrue();
        viewModel.Editor.MarkdownText.Should().Be("# Brugerens ændring");
        viewModel.HasDirtyEditors.Should().BeTrue();
        viewModel.ErrorMessage.Should().Contain("Kunne ikke gemme Markdown-fil");
        viewModel.ErrorMessage.Should().Contain("Simuleret skrivefejl");
    }

    [Fact]
    public async Task ConfirmCloseAsync_ShouldAllowClose_WithoutPrompt_WhenNothingIsDirty()
    {
        var unsavedChangesDialogService = new RecordingUnsavedChangesDialogService(UnsavedChangesDecision.Annullér);
        var viewModel = CreateViewModel(unsavedChangesDialogService: unsavedChangesDialogService);

        var canClose = await viewModel.ConfirmCloseAsync();

        canClose.Should().BeTrue();
        unsavedChangesDialogService.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmCloseAsync_ShouldSaveAndAllowClose_WhenUserChoosesGem()
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
        var markdownFileStore = new RecordingMarkdownFileStore(_ => "# Original tekst");
        var unsavedChangesDialogService = new RecordingUnsavedChangesDialogService(UnsavedChangesDecision.Gem);
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: markdownFileStore,
            unsavedChangesDialogService: unsavedChangesDialogService);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.Editor!.MarkdownText = "# Ændret";

        var canClose = await viewModel.ConfirmCloseAsync();

        canClose.Should().BeTrue();
        unsavedChangesDialogService.Calls.Should().Be(1);
        markdownFileStore.LastWriteContent.Should().Be("# Ændret");
        viewModel.HasDirtyEditors.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmCloseAsync_WhenSavingFails_ShouldCancelCloseAndKeepChanges()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: new FailingMarkdownFileStore(),
            unsavedChangesDialogService: new RecordingUnsavedChangesDialogService(
                UnsavedChangesDecision.Gem));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.Editor!.MarkdownText = "# Brugerens ændring";

        var canClose = await viewModel.ConfirmCloseAsync();

        canClose.Should().BeFalse();
        viewModel.HasDirtyEditors.Should().BeTrue();
        viewModel.ErrorMessage.Should().Contain("Simuleret skrivefejl");
    }

    [Fact]
    public async Task ConfirmCloseAsync_ShouldAllowClose_WhenUserChoosesKassér()
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
        var markdownFileStore = new RecordingMarkdownFileStore(_ => "# Original tekst");
        var unsavedChangesDialogService = new RecordingUnsavedChangesDialogService(UnsavedChangesDecision.Kassér);
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: markdownFileStore,
            unsavedChangesDialogService: unsavedChangesDialogService);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.Editor!.MarkdownText = "# Ændret";

        var canClose = await viewModel.ConfirmCloseAsync();

        canClose.Should().BeTrue();
        unsavedChangesDialogService.Calls.Should().Be(1);
        markdownFileStore.LastWriteContent.Should().BeNull();
        viewModel.HasDirtyEditors.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmCloseAsync_ShouldCancelClose_WhenUserChoosesAnnuller()
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
        var markdownFileStore = new RecordingMarkdownFileStore(_ => "# Original tekst");
        var unsavedChangesDialogService = new RecordingUnsavedChangesDialogService(UnsavedChangesDecision.Annullér);
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownFileStore: markdownFileStore,
            unsavedChangesDialogService: unsavedChangesDialogService);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.Editor!.MarkdownText = "# Ændret";

        var canClose = await viewModel.ConfirmCloseAsync();

        canClose.Should().BeFalse();
        unsavedChangesDialogService.Calls.Should().Be(1);
        markdownFileStore.LastWriteContent.Should().BeNull();
        viewModel.HasDirtyEditors.Should().BeTrue();
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
    public async Task SelectGedcomFileCommand_WhenKnownPersonChangesName_ShouldReuseExistingDocument()
    {
        using var firstFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        using var secondFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Andersen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]),
            settingsService: settingsService,
            markdownBiographyExportService: new MarkdownBiographyExportService(settingsService),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomSnapshotStore: new FileSystemGedcomSnapshotStore(),
            gedcomDifferenceDialogService: new ChoosingGedcomDifferenceDialogService(true));

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            viewModel.People.Should().ContainSingle();
            var originalPath = viewModel.People[0].MarkdownFilePath;
            viewModel.Editor!.MarkdownText += "\nBrugerens frie tekst.\n";
            viewModel.SaveAllCommand.Execute(null);

            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            Directory.GetFiles(outputFolder, "*.md").Should().ContainSingle().Which.Should().Be(originalPath);
            viewModel.People.Should().ContainSingle();
            viewModel.People[0].RecordId.Should().Be("@I1@");
            viewModel.People[0].DisplayName.Should().Be("Anna Andersen");
            viewModel.People[0].MarkdownFilePath.Should().Be(originalPath);
            viewModel.Editor.Should().NotBeNull();
            viewModel.Editor!.MarkdownText.Should().Contain("Brugerens frie tekst.");
        }
        finally
        {
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_WhenRecordIdHasDuplicateDocuments_ShouldMarkAmbiguousAndOpenNeither()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var metadata = new BiographyDocumentMetadata(
            1,
            "@I1@",
            "Anna Jensen",
            new BiographyFactsSnapshot("Anna Jensen", null, null, null, null, null, []));
        File.WriteAllText(
            Path.Combine(outputFolder, "anna-a.md"),
            BiographyDocumentSerializer.Serialize(metadata, "# Første dokument\n"));
        File.WriteAllText(
            Path.Combine(outputFolder, "anna-b.md"),
            BiographyDocumentSerializer.Serialize(metadata, "# Andet dokument\n"));
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog());

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            viewModel.People.Should().ContainSingle();
            viewModel.People[0].SyncStatus.Should().Be(BiographySyncStatus.Tvetydig);
            viewModel.People[0].SyncStatusText.Should().Be("Tvetydig");
            viewModel.Editor.Should().BeNull();
            viewModel.ErrorMessage.Should().Contain("flere Markdown-dokumenter");
            Directory.GetFiles(outputFolder, "*.md").Should().HaveCount(2);
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_AfterSecondImport_ShouldKeepDocumentsMissingFromLatestGedcomVisible()
    {
        using var firstFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        using var secondFile = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I2@ INDI",
            "1 NAME Bo /Jensen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]),
            settingsService: settingsService,
            markdownBiographyExportService: new MarkdownBiographyExportService(settingsService),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog());

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            viewModel.People.Select(person => person.RecordId)
                .Should().BeEquivalentTo("@I1@", "@I2@");
            Directory.GetFiles(outputFolder, "*.md").Should().HaveCount(2);
        }
        finally
        {
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_WhenExpectedFileHasUnknownVersion_ShouldPreserveAndBlockIt()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var tree = new GedcomLoader().Load(file.Path);
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var documentPath = Path.Combine(
            outputFolder,
            BiographyFileNameGenerator.Generate(tree.FindPerson("@I1@")!));
        const string content = "---\nformatVersion: 99\nrecordId: \"@I1@\"\n---\n# Bevar mig\n";
        File.WriteAllText(documentPath, content);
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            markdownFileStore: new FileSystemMarkdownFileStore());

        try
        {
            var action = () => viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            await action.Should().NotThrowAsync();
            File.ReadAllText(documentPath).Should().Be(content);
            Directory.GetFiles(outputFolder, "*.md").Should().ContainSingle();
            viewModel.People.Should().ContainSingle();
            viewModel.People[0].HasDocumentDiagnostic.Should().BeTrue();
            viewModel.People[0].DocumentErrorCategory.Should().Be("Ikke-understøttet formatversion");
            viewModel.Editor.Should().BeNull();
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_WhenOlderFormatMigrationIsApproved_ShouldUpgradeAndPreserveFreeText()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var tree = new GedcomLoader().Load(file.Path);
        var person = tree.FindPerson("@I1@")!;
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var documentPath = Path.Combine(outputFolder, BiographyFileNameGenerator.Generate(person));
        var oldMetadata = new BiographyDocumentMetadata(
            1,
            person.RecordId,
            person.FullName,
            BiographyFactsSnapshot.FromPerson(person));
        File.WriteAllText(
            documentPath,
            BiographyDocumentSerializer.Serialize(
                oldMetadata,
                "# Anna Jensen\n\n## Biografi\nBrugerens frie tekst.\n"));
        var differenceDialog = new ChoosingGedcomDifferenceDialogService(useGedcom: true);
        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: settingsService,
            markdownBiographyExportService: new MarkdownBiographyExportService(settingsService),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            gedcomDifferenceDialogService: differenceDialog);

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            differenceDialog.LastDifferences.Should().ContainSingle();
            differenceDialog.LastDifferences[0].RequiresMigration.Should().BeTrue();
            differenceDialog.LastDifferences[0].BaselineStatus.Should().Be(BiographyBaselineStatus.Missing);
            viewModel.HasDirtyEditors.Should().BeTrue();
            viewModel.SaveAllCommand.Execute(null);
            var migrated = BiographyDocumentParser.Parse(File.ReadAllText(documentPath));
            migrated.Metadata!.FormatVersion.Should().Be(BiographyDocumentParser.CurrentFormatVersion);
            migrated.Body.Should().Contain("Brugerens frie tekst.");
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldRequireReviewForUnknownBaselineVersion()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var tree = new GedcomLoader().Load(file.Path);
        var person = tree.FindPerson("@I1@")!;
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var settings = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = outputFolder,
        });
        var generator = new BiographyTemplateMarkdownGenerator();
        var generated = BiographyDocumentParser.Parse(generator.Generate(person));
        var unknownBaseline = generated.Metadata!.SyncBaseline! with { Version = 99 };
        var content = BiographyDocumentSerializer.Serialize(
            generated.Metadata with { SyncBaseline = unknownBaseline },
            generated.Body);
        var path = Path.Combine(outputFolder, BiographyFileNameGenerator.Generate(person));
        File.WriteAllText(path, content);
        var dialog = new RecordingGedcomDifferenceDialogService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: settings,
            markdownBiographyExportService: new MarkdownBiographyExportService(settings),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            gedcomDifferenceDialogService: dialog);

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            dialog.LastDifferences.Should().ContainSingle();
            dialog.LastDifferences[0].BaselineStatus.Should().Be(BiographyBaselineStatus.UnsupportedVersion);
            dialog.LastDifferences[0].RequiresMigration.Should().BeTrue();
            File.ReadAllText(path).Should().Be(content);
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
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
    public async Task SelectGedcomFileCommand_WhileReviewWaits_ShouldNotWriteAndCanBeCancelled()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var outputFolder = CreateBiographyWorkspace("@I1@", "Gammelt navn", "anna.md");
        var originalFiles = SnapshotFiles(outputFolder);
        var snapshotStore = new RecordingGedcomSnapshotStore();
        var dialog = new BlockingGedcomDifferenceDialogService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownBiographyExportService: new MarkdownBiographyExportService(
                new RecordingApplicationSettingsService(new AppSettings())),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomDifferenceDialogService: dialog,
            gedcomSnapshotStore: snapshotStore);
        var originalPeople = viewModel.People.Select(person => person.RecordId).ToArray();

        try
        {
            var importTask = viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            viewModel.IsImporting.Should().BeTrue();
            viewModel.ImportPhaseText.Should().Be("Gennemgang");
            viewModel.SelectGedcomFileCommand.CanExecute(null).Should().BeFalse();
            viewModel.CancelImportCommand.CanExecute(null).Should().BeTrue();
            snapshotStore.SaveCalls.Should().Be(0);
            SnapshotFiles(outputFolder).Should().BeEquivalentTo(originalFiles);

            viewModel.CancelImportCommand.Execute(null);
            await importTask;

            viewModel.IsImporting.Should().BeFalse();
            viewModel.ImportPhaseText.Should().Be("Annulleret");
            viewModel.ErrorMessage.Should().Contain("annulleret");
            snapshotStore.SaveCalls.Should().Be(0);
            SnapshotFiles(outputFolder).Should().BeEquivalentTo(originalFiles);
            viewModel.People.Select(person => person.RecordId).Should().Equal(originalPeople);
        }
        finally
        {
            dialog.Complete(null);
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_WhenStartedInParallel_ShouldRunLoaderOnlyOnce()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var loader = new BlockingGedcomLoader(file.Path);
        var viewModel = CreateViewModel(
            gedcomLoader: loader,
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }));

        try
        {
            var firstImport = viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            await loader.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var secondImport = viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            await secondImport;

            loader.Calls.Should().Be(1);
            viewModel.IsImporting.Should().BeTrue();

            loader.Release();
            await firstImport;
        }
        finally
        {
            loader.Release();
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_WhenCommitFails_ShouldRollbackFilesAndKeepPublishedState()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I2@ INDI",
            "1 NAME Bo /Jensen/",
            "0 TRLR");
        var outputFolder = CreateBiographyWorkspace("@I1@", "Anna Jensen", "anna.md");
        var originalFiles = SnapshotFiles(outputFolder);
        var snapshotStore = new RecordingGedcomSnapshotStore();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownBiographyExportService: new PartiallyFailingMarkdownBiographyExportService(),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomSnapshotStore: snapshotStore);
        var originalPeople = viewModel.People.Select(person => person.RecordId).ToArray();
        var originalSelectedGedcom = viewModel.SelectedGedcomFilePath;

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            viewModel.ImportPhaseText.Should().Be("Fejl");
            viewModel.ErrorMessage.Should().Contain("rullet tilbage");
            snapshotStore.SaveCalls.Should().Be(0);
            SnapshotFiles(outputFolder).Should().BeEquivalentTo(originalFiles);
            viewModel.People.Select(person => person.RecordId).Should().Equal(originalPeople);
            viewModel.SelectedGedcomFilePath.Should().Be(originalSelectedGedcom);
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_WhenTemplatePreflightFails_ShouldNotCommitAnything()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var outputFolder = CreateBiographyWorkspace("@I2@", "Bo Jensen", "bo.md");
        var originalFiles = SnapshotFiles(outputFolder);
        var snapshotStore = new RecordingGedcomSnapshotStore();
        var exporter = new InvalidCandidateMarkdownBiographyExportService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownBiographyExportService: exporter,
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomSnapshotStore: snapshotStore);
        var originalPeople = viewModel.People.Select(person => person.RecordId).ToArray();

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            viewModel.ImportPhaseText.Should().Be("Fejl");
            viewModel.HasImportStatus.Should().BeTrue();
            viewModel.ErrorMessage.Should().Contain("Forhåndskontrollen");
            snapshotStore.SaveCalls.Should().Be(0);
            exporter.WriteCalls.Should().Be(0);
            SnapshotFiles(outputFolder).Should().BeEquivalentTo(originalFiles);
            viewModel.People.Select(person => person.RecordId).Should().Equal(originalPeople);
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_WhenSnapshotCommitFails_ShouldRollbackNewDocuments()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            markdownBiographyExportService: new MarkdownBiographyExportService(
                new RecordingApplicationSettingsService(new AppSettings())),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomSnapshotStore: new FailingSaveGedcomSnapshotStore());

        try
        {
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            viewModel.ImportPhaseText.Should().Be("Fejl");
            viewModel.ErrorMessage.Should().Contain("rullet tilbage");
            Directory.GetFiles(outputFolder, "*.md").Should().BeEmpty();
            SnapshotFiles(outputFolder).Should().BeEmpty();
            Directory.Exists(Path.Combine(outputFolder, ".slaegtsassistent")).Should().BeFalse();
            viewModel.People.Should().BeEmpty();
            viewModel.SelectedGedcomFilePath.Should().BeNull();
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
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
    public async Task OpenSettingsCommand_WhenStorageFails_ShouldExposeDanishErrorWithoutThrowing()
    {
        var settingsDialog = new RecordingSettingsDialogService(new AppSettings
        {
            DefaultMarkdownOutputFolder = "/tmp/ny-outputmappe",
        });
        var viewModel = CreateViewModel(
            settingsDialogService: settingsDialog,
            settingsService: new FailingApplicationSettingsService());

        var action = () => viewModel.OpenSettingsCommand.ExecuteAsync(null);

        await action.Should().NotThrowAsync();
        viewModel.ErrorMessage.Should().Contain("Kunne ikke gemme indstillinger");
        viewModel.ErrorMessage.Should().Contain("/tmp/settings.json");
        viewModel.ErrorMessage.Should().Contain("prøv igen");
    }

    [Fact]
    public async Task OpenSettingsCommand_WhenDirtyWorkspaceSwitchIsCancelled_ShouldKeepOldWorkspace()
    {
        var oldFolder = CreateBiographyWorkspace("@I1@", "Anna Jensen", "anna.md");
        var newFolder = CreateBiographyWorkspace("@I2@", "Bo Jensen", "bo.md");
        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = oldFolder,
        });
        var viewModel = CreateViewModel(
            settingsService: settingsService,
            settingsDialogService: new RecordingSettingsDialogService(new AppSettings
            {
                DefaultMarkdownOutputFolder = newFolder,
            }),
            unsavedChangesDialogService: new RecordingUnsavedChangesDialogService(
                UnsavedChangesDecision.Annullér),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog());

        try
        {
            viewModel.SelectedPerson = viewModel.People.Single();
            viewModel.Editor!.MarkdownText += "\nUgemt tekst.";

            await viewModel.OpenSettingsCommand.ExecuteAsync(null);

            viewModel.StandardMarkdownOutputFolder.Should().Be(oldFolder);
            viewModel.People.Should().ContainSingle(person => person.RecordId == "@I1@");
            viewModel.Editor.Should().NotBeNull();
            viewModel.Editor!.IsDirty.Should().BeTrue();
            settingsService.SavedSettings.Should().BeNull();
        }
        finally
        {
            Directory.Delete(oldFolder, recursive: true);
            Directory.Delete(newFolder, recursive: true);
        }
    }

    [Fact]
    public async Task OpenSettingsCommand_WhenDirtyWorkspaceIsDiscarded_ShouldActivateNewWorkspace()
    {
        var oldFolder = CreateBiographyWorkspace("@I1@", "Anna Jensen", "anna.md");
        var newFolder = CreateBiographyWorkspace("@I2@", "Bo Jensen", "bo.md");
        var settingsService = new RecordingApplicationSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = oldFolder,
        });
        var viewModel = CreateViewModel(
            settingsService: settingsService,
            settingsDialogService: new RecordingSettingsDialogService(new AppSettings
            {
                DefaultMarkdownOutputFolder = newFolder,
            }),
            unsavedChangesDialogService: new RecordingUnsavedChangesDialogService(
                UnsavedChangesDecision.Kassér),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog());

        try
        {
            viewModel.SelectedPerson = viewModel.People.Single();
            viewModel.Editor!.MarkdownText += "\nDenne tekst skal kasseres.";

            await viewModel.OpenSettingsCommand.ExecuteAsync(null);

            viewModel.StandardMarkdownOutputFolder.Should().Be(newFolder);
            viewModel.People.Should().ContainSingle(person => person.RecordId == "@I2@");
            viewModel.People.Should().NotContain(person => person.RecordId == "@I1@");
            viewModel.Editor.Should().NotBeNull();
            viewModel.Editor!.IsDirty.Should().BeFalse();
            viewModel.HasDirtyEditors.Should().BeFalse();
            File.ReadAllText(Path.Combine(oldFolder, "anna.md"))
                .Should().NotContain("Denne tekst skal kasseres.");
        }
        finally
        {
            Directory.Delete(oldFolder, recursive: true);
            Directory.Delete(newFolder, recursive: true);
        }
    }

    [Fact]
    public async Task OpenSettingsCommand_WhenDirtyWorkspaceIsSaved_ShouldPersistBeforeActivatingNewWorkspace()
    {
        var oldFolder = CreateBiographyWorkspace("@I1@", "Anna Jensen", "anna.md");
        var newFolder = CreateBiographyWorkspace("@I2@", "Bo Jensen", "bo.md");
        var viewModel = CreateViewModel(
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = oldFolder,
            }),
            settingsDialogService: new RecordingSettingsDialogService(new AppSettings
            {
                DefaultMarkdownOutputFolder = newFolder,
            }),
            unsavedChangesDialogService: new RecordingUnsavedChangesDialogService(
                UnsavedChangesDecision.Gem),
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog());

        try
        {
            viewModel.SelectedPerson = viewModel.People.Single();
            viewModel.Editor!.MarkdownText += "\nTekst gemt før skift.";

            await viewModel.OpenSettingsCommand.ExecuteAsync(null);

            File.ReadAllText(Path.Combine(oldFolder, "anna.md"))
                .Should().Contain("Tekst gemt før skift.");
            viewModel.StandardMarkdownOutputFolder.Should().Be(newFolder);
            viewModel.People.Should().ContainSingle(person => person.RecordId == "@I2@");
            viewModel.HasDirtyEditors.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(oldFolder, recursive: true);
            Directory.Delete(newFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_AfterWorkspaceSwitch_ShouldUseOnlyNewWorkspace()
    {
        using var file = CreateTemporaryGedcomFile(
            "0 HEAD",
            "0 @I2@ INDI",
            "1 NAME Bo /Jensen/",
            "0 TRLR");
        var oldFolder = CreateBiographyWorkspace("@I1@", "Anna Jensen", "anna.md");
        var newFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(newFolder);
        var oldContent = File.ReadAllText(Path.Combine(oldFolder, "anna.md"));
        var exporter = new RecordingMarkdownBiographyExportService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = oldFolder,
            }),
            settingsDialogService: new RecordingSettingsDialogService(new AppSettings
            {
                DefaultMarkdownOutputFolder = newFolder,
            }),
            markdownBiographyExportService: exporter,
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog());

        try
        {
            await viewModel.OpenSettingsCommand.ExecuteAsync(null);
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            exporter.LastOutputFolder.Should().Be(newFolder);
            viewModel.People.Should().ContainSingle();
            viewModel.People[0].MarkdownFilePath.Should().StartWith(newFolder);
            File.ReadAllText(Path.Combine(oldFolder, "anna.md")).Should().Be(oldContent);
            Directory.Exists(Path.Combine(newFolder, ".slaegtsassistent", "gedcom"))
                .Should().BeTrue();
        }
        finally
        {
            Directory.Delete(oldFolder, recursive: true);
            Directory.Delete(newFolder, recursive: true);
        }
    }

    [Fact]
    public async Task ExitApplicationCommand_ShouldCallApplicationControlService_WhenNothingIsDirty()
    {
        var applicationControlService = new RecordingApplicationControlService();
        var viewModel = CreateViewModel(applicationControlService: applicationControlService);

        await viewModel.ExitApplicationCommand.ExecuteAsync(null);

        applicationControlService.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ExitApplicationCommand_ShouldNotExit_WhenUserCancelsUnsavedChanges()
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
        var applicationControlService = new RecordingApplicationControlService();
        var viewModel = CreateViewModel(
            gedcomFilePickerService: new FakeGedcomFilePickerService(file.Path),
            gedcomLoader: new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            settingsService: new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = outputFolder,
            }),
            applicationControlService: applicationControlService,
            unsavedChangesDialogService: new RecordingUnsavedChangesDialogService(UnsavedChangesDecision.Annullér));

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        viewModel.Editor!.MarkdownText = "# Ændret";

        await viewModel.ExitApplicationCommand.ExecuteAsync(null);

        applicationControlService.Calls.Should().Be(0);
        viewModel.HasDirtyEditors.Should().BeTrue();
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldNotCommitPartialImport_WhenUserRejectsReport()
    {
        using var gedcom = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 INDI",
            "1 NAME Mangler /Id/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var export = new RecordingMarkdownBiographyExportService();
        var snapshot = new RecordingGedcomSnapshotStore();
        var partialDialog = new RecordingPartialImportDialogService(false);

        try
        {
            var viewModel = CreateViewModel(
                gedcomLoader: new GedcomLoader(),
                gedcomFilePickerService: new FakeGedcomFilePickerService(gedcom.Path),
                settingsService: new RecordingApplicationSettingsService(new AppSettings
                {
                    DefaultMarkdownOutputFolder = outputFolder,
                }),
                markdownBiographyExportService: export,
                gedcomSnapshotStore: snapshot,
                partialImportDialogService: partialDialog);

            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            partialDialog.Calls.Should().Be(1);
            partialDialog.LastReport!.SkippedRecords.Should().Be(1);
            export.Calls.Should().Be(0);
            snapshot.SaveCalls.Should().Be(0);
            viewModel.People.Should().BeEmpty();
            viewModel.SelectedGedcomFilePath.Should().BeNull();
            viewModel.ImportPhaseText.Should().Be("Afvist");
            viewModel.ErrorMessage.Should().Contain("Arbejdsområdets filer og aktive data er uændrede");
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldCommitAndPublishReport_WhenPartialImportIsAccepted()
    {
        using var gedcom = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "0 INDI",
            "1 NAME Mangler /Id/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var snapshot = new RecordingGedcomSnapshotStore();
        var partialDialog = new RecordingPartialImportDialogService(true);

        try
        {
            var viewModel = CreateViewModel(
                gedcomLoader: new GedcomLoader(),
                gedcomFilePickerService: new FakeGedcomFilePickerService(gedcom.Path),
                settingsService: new RecordingApplicationSettingsService(new AppSettings
                {
                    DefaultMarkdownOutputFolder = outputFolder,
                }),
                gedcomSnapshotStore: snapshot,
                partialImportDialogService: partialDialog);

            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            snapshot.SaveCalls.Should().Be(1);
            viewModel.People.Should().ContainSingle(person => person.RecordId == "@I1@");
            viewModel.ImportSummaryText.Should().Be(
                "Importerede: 1 · Med advarsler: 0 · Oversprungne: 1 · Fatale: 0");
            viewModel.ImportDiagnostics.Should().ContainSingle();
            viewModel.HasImportDiagnostics.Should().BeTrue();
            viewModel.ImportPhaseText.Should().Be("Færdig");
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task ImportDiagnostics_ShouldFilterAndNavigateToRelevantPerson()
    {
        using var gedcom = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "ugyldig linje",
            "1 _UKENDT Bevares",
            "0 @I2@ INDI",
            "1 NAME Bent /Jensen/",
            "0 TRLR");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);

        try
        {
            var viewModel = CreateViewModel(
                gedcomLoader: new GedcomLoader(),
                gedcomFilePickerService: new FakeGedcomFilePickerService(gedcom.Path),
                settingsService: new RecordingApplicationSettingsService(new AppSettings
                {
                    DefaultMarkdownOutputFolder = outputFolder,
                }),
                partialImportDialogService: new RecordingPartialImportDialogService(true));

            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            viewModel.ImportDiagnostics.Should().HaveCount(2);
            viewModel.SelectedDiagnosticSeverityFilter = "Fejl";
            viewModel.ImportDiagnostics.Should().ContainSingle(item => item.Message.Contains("ugyldig syntaks"));
            viewModel.SelectedPerson = viewModel.People.Single(person => person.RecordId == "@I2@");
            viewModel.SelectedImportDiagnostic = viewModel.ImportDiagnostics.Single();
            viewModel.SelectedPerson!.RecordId.Should().Be("@I1@");
            viewModel.SelectedDiagnosticSeverityFilter = "Advarsler";
            viewModel.ImportDiagnostics.Should().ContainSingle(item => item.Message.Contains("_UKENDT"));
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    [Fact]
    public async Task SelectGedcomFileCommand_ShouldPublishFatalReportWithoutCommit()
    {
        using var validGedcom = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I0@ INDI",
            "1 NAME Eksisterende /Person/",
            "0 TRLR");
        using var fatalGedcom = CreateTemporaryGedcomFile(
            "0 HEAD",
            "1 CHAR UTF-8",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/");
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        var snapshot = new RecordingGedcomSnapshotStore();
        var partialDialog = new RecordingPartialImportDialogService(true);

        try
        {
            var viewModel = CreateViewModel(
                gedcomLoader: new GedcomLoader(),
                gedcomFilePickerService: new SequencedGedcomFilePickerService(
                    [validGedcom.Path, fatalGedcom.Path]),
                settingsService: new RecordingApplicationSettingsService(new AppSettings
                {
                    DefaultMarkdownOutputFolder = outputFolder,
                }),
                gedcomSnapshotStore: snapshot,
                partialImportDialogService: partialDialog);

            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
            viewModel.People.Should().ContainSingle(person => person.RecordId == "@I0@");

            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            partialDialog.Calls.Should().Be(0);
            snapshot.SaveCalls.Should().Be(1);
            viewModel.ImportPhaseText.Should().Be("Fejl");
            viewModel.ImportSummaryText.Should().Be(
                "Importerede: 0 · Med advarsler: 0 · Oversprungne: 0 · Fatale: 1");
            viewModel.ImportDiagnostics.Should().ContainSingle(item =>
                item.Severity == GedcomDiagnosticSeverity.Fatal
                && item.Diagnostic.Tag == "TRLR");
            viewModel.People.Should().ContainSingle(person => person.RecordId == "@I0@");
            viewModel.SelectedGedcomFilePath.Should().Be(validGedcom.Path);
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    private static MainWindowViewModel CreateViewModel(
        IGedcomLoader? gedcomLoader = null,
        IGedcomFilePickerService? gedcomFilePickerService = null,
        IFolderPickerService? folderPickerService = null,
        IApplicationSettingsService? settingsService = null,
        ISettingsDialogService? settingsDialogService = null,
        IUserDialogService? userDialogService = null,
        IUnsavedChangesDialogService? unsavedChangesDialogService = null,
        IApplicationControlService? applicationControlService = null,
        IMarkdownBiographyExportService? markdownBiographyExportService = null,
        IMarkdownFileStore? markdownFileStore = null,
        IMarkdownDocumentCatalog? markdownDocumentCatalog = null,
        IGedcomDifferenceDialogService? gedcomDifferenceDialogService = null,
        IGedcomSnapshotStore? gedcomSnapshotStore = null,
        IPartialImportDialogService? partialImportDialogService = null)
    {
        return new MainWindowViewModel(
            gedcomLoader ?? new RecordingGedcomLoader(path => new GedcomLoader().Load(path)),
            gedcomFilePickerService ?? new FakeGedcomFilePickerService(null),
            folderPickerService ?? new RecordingFolderPickerService(null),
            settingsService ?? new RecordingApplicationSettingsService(new AppSettings()),
            settingsDialogService ?? new RecordingSettingsDialogService(null),
            userDialogService ?? new NullUserDialogService(),
            unsavedChangesDialogService ?? new RecordingUnsavedChangesDialogService(UnsavedChangesDecision.Annullér),
            applicationControlService ?? new RecordingApplicationControlService(),
            markdownBiographyExportService ?? new RecordingMarkdownBiographyExportService(),
            markdownFileStore ?? new RecordingMarkdownFileStore(),
            markdownDocumentCatalog,
            gedcomDifferenceDialogService,
            gedcomSnapshotStore: gedcomSnapshotStore,
            partialImportDialogService: partialImportDialogService);
    }

    private static TemporaryGedcomFile CreateTemporaryGedcomFile(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllLines(filePath, lines);
        return new TemporaryGedcomFile(filePath);
    }

    private static string CreateBiographyWorkspace(string recordId, string displayName, string fileName)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var metadata = new BiographyDocumentMetadata(
            1,
            recordId,
            displayName,
            new BiographyFactsSnapshot(displayName, null, null, null, null, null, []));
        File.WriteAllText(
            Path.Combine(folder, fileName),
            BiographyDocumentSerializer.Serialize(metadata, $"# {displayName}\n"));
        return folder;
    }

    private static Dictionary<string, byte[]> SnapshotFiles(string folder)
    {
        return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(folder, path),
                File.ReadAllBytes,
                StringComparer.Ordinal);
    }

    private static TemporaryGedcomFile CreateTemporaryGedcomFileWithoutExtension(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
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

    private sealed class FailingApplicationSettingsService : IApplicationSettingsService
    {
        public AppSettings Load() => new();

        public void Save(AppSettings settings)
        {
            throw new AtomicFileWriteException(
                "/tmp/settings.json",
                new IOException("Simuleret skrivefejl."));
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

        public string GenerateBiography(
            FamilyTree familyTree,
            Person person,
            string outputDirectory)
        {
            return new BiographyTemplateMarkdownGenerator().Generate(person);
        }
    }

    private sealed class PartiallyFailingMarkdownBiographyExportService : IMarkdownBiographyExportService
    {
        public void WriteBiographies(FamilyTree familyTree, string outputDirectory)
        {
            File.WriteAllText(Path.Combine(outputDirectory, "delvis-oprettet.md"), "Delvist indhold");
            throw new IOException("Simuleret fejl under gennemførelsen.");
        }

        public string GenerateBiography(FamilyTree familyTree, Person person, string outputDirectory)
        {
            return new BiographyTemplateMarkdownGenerator().Generate(person);
        }
    }

    private sealed class InvalidCandidateMarkdownBiographyExportService : IMarkdownBiographyExportService
    {
        public int WriteCalls { get; private set; }

        public void WriteBiographies(FamilyTree familyTree, string outputDirectory)
        {
            WriteCalls++;
        }

        public string GenerateBiography(FamilyTree familyTree, Person person, string outputDirectory)
        {
            return "# Kandidat uden metadata\n";
        }
    }

    private sealed class RecordingGedcomDifferenceDialogService : IGedcomDifferenceDialogService
    {
        public int Calls { get; private set; }

        public IReadOnlyList<GedcomDifferenceReviewItem> LastDifferences { get; private set; } = [];

        public Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
            IReadOnlyList<GedcomDifferenceReviewItem> differences)
        {
            Calls++;
            LastDifferences = differences;
            return Task.FromResult<IReadOnlyDictionary<string, bool>?>(null);
        }
    }

    private sealed class BlockingGedcomDifferenceDialogService : IGedcomDifferenceDialogService
    {
        private readonly TaskCompletionSource<IReadOnlyDictionary<string, bool>?> _completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
            IReadOnlyList<GedcomDifferenceReviewItem> differences)
        {
            Started.TrySetResult();
            return _completion.Task;
        }

        public void Complete(IReadOnlyDictionary<string, bool>? result)
        {
            _completion.TrySetResult(result);
        }
    }

    private sealed class ChoosingGedcomDifferenceDialogService : IGedcomDifferenceDialogService
        {
            private readonly bool _useGedcom;

            public ChoosingGedcomDifferenceDialogService(bool useGedcom)
            {
                _useGedcom = useGedcom;
            }

            public IReadOnlyList<GedcomDifferenceReviewItem> LastDifferences { get; private set; } = [];

            public Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
                IReadOnlyList<GedcomDifferenceReviewItem> differences)
            {
                LastDifferences = differences;
                return Task.FromResult<IReadOnlyDictionary<string, bool>?>(
                    differences.ToDictionary(item => item.Key, _ => _useGedcom, StringComparer.Ordinal));
            }
    }

    private sealed class PathChoosingGedcomDifferenceDialogService(string selectedPath)
        : IGedcomDifferenceDialogService
    {
        public IReadOnlyList<GedcomDifferenceReviewItem> LastDifferences { get; private set; } = [];

        public Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
            IReadOnlyList<GedcomDifferenceReviewItem> differences)
        {
            LastDifferences = differences;
            return Task.FromResult<IReadOnlyDictionary<string, bool>?>(differences.ToDictionary(
                item => item.Key,
                item => string.Equals(item.StructuredDifference?.Path, selectedPath, StringComparison.Ordinal),
                StringComparer.Ordinal));
        }
    }

    private sealed class ThrowingGedcomSnapshotStore : IGedcomSnapshotStore
    {
        public GedcomSnapshot? Load(string? outputDirectory)
        {
            throw new GedcomSnapshotException("Snapshot er ugyldigt.");
        }

        public void Save(string outputDirectory, string sourcePath, FamilyTree familyTree)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingGedcomSnapshotStore : IGedcomSnapshotStore
    {
        public int SaveCalls { get; private set; }

        public GedcomSnapshot? Load(string? outputDirectory) => null;

        public void Save(string outputDirectory, string sourcePath, FamilyTree familyTree)
        {
            SaveCalls++;
        }
    }

    private sealed class FailingSaveGedcomSnapshotStore : IGedcomSnapshotStore
    {
        public GedcomSnapshot? Load(string? outputDirectory) => null;

        public void Save(string outputDirectory, string sourcePath, FamilyTree familyTree)
        {
            var snapshotDirectory = Path.Combine(outputDirectory, ".slaegtsassistent", "gedcom");
            Directory.CreateDirectory(snapshotDirectory);
            File.WriteAllText(Path.Combine(snapshotDirectory, "manifest.json"), "delvist snapshot");
            throw new GedcomSnapshotException("Simuleret snapshotfejl.");
        }
    }

    private sealed class RecordingUnsavedChangesDialogService : IUnsavedChangesDialogService
    {
        private readonly UnsavedChangesDecision _result;

        public RecordingUnsavedChangesDialogService(UnsavedChangesDecision result)
        {
            _result = result;
        }

        public int Calls { get; private set; }

        public Task<UnsavedChangesDecision> AskAsync()
        {
            Calls++;
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingPartialImportDialogService(bool result) : IPartialImportDialogService
    {
        public int Calls { get; private set; }

        public GedcomImportReport? LastReport { get; private set; }

        public Task<bool> ConfirmAsync(GedcomImportReport report)
        {
            Calls++;
            LastReport = report;
            return Task.FromResult(result);
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
        public string? LastWritePath { get; private set; }
        public string? LastWriteContent { get; private set; }
        public List<(string Path, string Content)> Writes { get; } = [];

        public string Read(string path)
        {
            LastReadPath = path;
            return _read(path);
        }

        public void Write(string path, string content)
        {
            LastWritePath = path;
            LastWriteContent = content;
            Writes.Add((path, content));
        }
    }

    private sealed class FailingMarkdownFileStore : IMarkdownFileStore
    {
        public string Read(string path) => "# Oprindelig tekst";

        public void Write(string path, string content)
        {
            throw new IOException("Simuleret skrivefejl.");
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

    private sealed class BlockingGedcomLoader(string sourcePath) : IGedcomLoader
    {
        private readonly ManualResetEventSlim _release = new(false);

        public TaskCompletionSource Started { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls { get; private set; }

        public FamilyTree Load(string filePath, FamilyTree? existingTree = null)
        {
            Calls++;
            Started.TrySetResult();
            _release.Wait(TimeSpan.FromSeconds(10));
            return new GedcomLoader().Load(sourcePath);
        }

        public void Release() => _release.Set();
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
