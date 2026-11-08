using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

public partial class MainWindowViewModelTests
{
    [Fact]
    public async Task CreatingTemplateBeforeFirstImport_ShouldLeaveGedcomSelectionAvailable()
    {
        using var gedcom = CreateTwoPersonGedcomFile();
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var outputFolder = Path.Combine(folder, "dokumenter");
            var settings = new JsonApplicationSettingsService(Path.Combine(folder, "indstillinger.json"));
            settings.Save(new AppSettings { DefaultMarkdownOutputFolder = outputFolder });
            var path = Path.Combine(folder, "ny.slaegtskabelon");
            var model = BiographyTemplateEditorModel.CreateDefault();
            new TemplateEditorStorageService().Save(path, new(model.Version, model.Blocks.Select(block =>
                block.Type == BiographyTemplateBlockType.Facts ? block with { Heading = "Mine fakta" } : block).ToArray()));
            var viewModel = CreateViewModel(settingsService: settings,
                settingsDialogService: new RecordingSettingsDialogService(new AppSettings
                {
                    DefaultMarkdownOutputFolder = outputFolder,
                    GlobalBiographyTemplatePath = path,
                }),
                gedcomFilePickerService: new FakeGedcomFilePickerService(gedcom.Path),
                markdownBiographyExportService: new MarkdownBiographyExportService(settings),
                markdownFileStore: new FileSystemMarkdownFileStore());

            await viewModel.OpenSettingsCommand.ExecuteAsync(null);
            viewModel.SelectGedcomFileCommand.CanExecute(null).Should().BeTrue();
            await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

            viewModel.ErrorMessage.Should().BeNull();
            viewModel.People.Should().HaveCount(2);
            Directory.GetFiles(outputFolder, "*.md").Select(File.ReadAllText)
                .Should().OnlyContain(content => content.Contains("## Mine fakta"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public async Task ApplyTemplate_ShouldAllowCancellationWithoutWritingDocuments()
    {
        using var workspace = new TemplateUpdateWorkspace();
        var dialog = new BlockingGedcomDifferenceDialogService();
        var viewModel = CreateViewModel(settingsService: workspace.Settings,
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomDifferenceDialogService: dialog);

        var apply = viewModel.ApplyBiographyTemplateCommand.ExecuteAsync(null);
        await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        viewModel.SelectGedcomFileCommand.CanExecute(null).Should().BeFalse();
        viewModel.OpenSettingsCommand.CanExecute(null).Should().BeFalse();
        viewModel.CancelImportCommand.Execute(null);
        await apply;
        dialog.Complete(null);

        viewModel.IsImporting.Should().BeFalse();
        viewModel.ImportPhaseText.Should().Be("Skabelonændringer annulleret");
        viewModel.SelectGedcomFileCommand.CanExecute(null).Should().BeTrue();
        SnapshotFiles(workspace.Folder).Should().BeEquivalentTo(workspace.Original);
    }

    [Fact]
    public async Task ApplyTemplate_ShouldNotOverwriteEditsMadeDuringReview()
    {
        using var workspace = new TemplateUpdateWorkspace();
        var dialog = new BlockingGedcomDifferenceDialogService();
        var viewModel = CreateViewModel(settingsService: workspace.Settings,
            markdownFileStore: new FileSystemMarkdownFileStore(),
            markdownDocumentCatalog: new FileSystemMarkdownDocumentCatalog(),
            gedcomDifferenceDialogService: dialog);
        viewModel.SelectedPerson = viewModel.People.First();
        var editor = viewModel.Editor!;

        var apply = viewModel.ApplyBiographyTemplateCommand.ExecuteAsync(null);
        await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        editor.MarkdownText += "\nNy tekst skrevet under gennemgangen.";
        var choices = viewModel.People.ToDictionary(person => $"{person.MarkdownFilePath}|template", _ => true);
        dialog.Complete(choices);
        await apply;

        viewModel.ErrorMessage.Should().Contain("ændret under gennemgangen");
        editor.MarkdownText.Should().Contain("Ny tekst skrevet under gennemgangen.");
        editor.IsDirty.Should().BeTrue();
        SnapshotFiles(workspace.Folder).Should().BeEquivalentTo(workspace.Original);
    }

    [Fact]
    public void ApplyTemplate_ShouldBeDisabledWithoutDocuments()
    {
        var viewModel = CreateViewModel();

        viewModel.ApplyBiographyTemplateCommand.CanExecute(null).Should().BeFalse();
    }

    private sealed class TemplateUpdateWorkspace : IDisposable
    {
        public string Folder { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        public RecordingApplicationSettingsService Settings { get; }
        public Dictionary<string, byte[]> Original { get; }

        public TemplateUpdateWorkspace()
        {
            Directory.CreateDirectory(Folder);
            using var gedcom = CreateTwoPersonGedcomFile();
            var path = Path.Combine(Folder, "ny.slaegtskabelon");
            Settings = new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = Folder,
            });
            new MarkdownBiographyExportService(Settings).WriteBiographies(new GedcomLoader().Load(gedcom.Path), Folder);
            var model = BiographyTemplateEditorModel.CreateDefault();
            new TemplateEditorStorageService().Save(path, new(model.Version, model.Blocks.Select(block =>
                block.Type == BiographyTemplateBlockType.Facts ? block with { Heading = "Mine fakta" } : block).ToArray()));
            Settings = new RecordingApplicationSettingsService(new AppSettings
            {
                DefaultMarkdownOutputFolder = Folder,
                GlobalBiographyTemplatePath = path,
            });
            Original = SnapshotFiles(Folder);
        }

        public void Dispose() => Directory.Delete(Folder, true);
    }
}
