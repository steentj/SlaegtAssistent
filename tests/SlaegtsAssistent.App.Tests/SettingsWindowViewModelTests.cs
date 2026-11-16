using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class SettingsWindowViewModelTests
{
    [Fact]
    public async Task ValgtMediemappeGemmesOgFinderEksporteretGedcomFil()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var gedcom = Directory.CreateDirectory(Path.Combine(root, "gedcom")).FullName;
        var media = Directory.CreateDirectory(Path.Combine(root, "Medier")).FullName;
        var docs = Directory.CreateDirectory(Path.Combine(root, "dokumenter")).FullName;
        try
        {
            File.WriteAllText(Path.Combine(media, "anna.jpg"), "syntetisk fil");
            var settingsPath = Path.Combine(root, "indstillinger.json");
            var settingsService = new JsonApplicationSettingsService(settingsPath, new AtomicFileWriter());
            var viewModel = new SettingsWindowViewModel(settingsService.Load(), new ChosenFolderPicker(media));
            AppSettings? saved = null;
            viewModel.CloseRequested += (_, settings) => saved = settings;
            await viewModel.SelectMediaFolderCommand.ExecuteAsync(null);
            await viewModel.SaveCommand.ExecuteAsync(null);
            saved.Should().NotBeNull();
            settingsService.Save(saved!);
            var reopened = settingsService.Load();
            reopened.DefaultMediaFolder.Should().Be(media);
            var index = await MediaFileIndex.BuildAsync(reopened.DefaultMediaFolder!, StringComparer.Ordinal);
            var resolved = new BiographyMediaResolver().Resolve("Medier/anna.jpg", gedcom, docs, reopened.DefaultMediaFolder, index);
            resolved.ResolvedPath.Should().Be(Path.Combine(media, "anna.jpg"));
            resolved.Diagnostic.Should().BeNull();
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class ChosenFolderPicker(string folder) : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder) => Task.FromResult<string?>(folder);
    }

    [Fact]
    public void IndstillingerKanLaeseTidligereJsonOgGemmeMediemappe()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "indstillinger.json");
        try
        {
            File.WriteAllText(path, "{\"DefaultGedcomInputFolder\":null}");
            var service = new JsonApplicationSettingsService(path, new AtomicFileWriter());
            var settings = service.Load();
            settings.DefaultMediaFolder.Should().BeNull();
            settings.DefaultMediaFolder = directory;
            service.Save(settings);
            service.Load().DefaultMediaFolder.Should().Be(directory);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task MediemappeKanGemmesOgRyddes()
    {
        var folder = Path.GetTempPath();
        var viewModel = new SettingsWindowViewModel(new AppSettings(), new StubFolderPickerService());
        AppSettings? saved = null;
        viewModel.CloseRequested += (_, settings) => saved = settings;
        viewModel.DefaultMediaFolder = folder;
        await viewModel.SaveCommand.ExecuteAsync(null);
        saved!.DefaultMediaFolder.Should().Be(folder);
        viewModel.ClearMediaFolderCommand.Execute(null);
        viewModel.DefaultMediaFolder.Should().BeNull();
        await viewModel.SaveCommand.ExecuteAsync(null);
        saved!.DefaultMediaFolder.Should().BeNull();
    }

    [Fact]
    public async Task NyUgyldigMediemappeAfvisesMenTidligereUtilgaengeligBevares()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var viewModel = new SettingsWindowViewModel(new AppSettings(), new StubFolderPickerService());
        AppSettings? saved = null;
        viewModel.CloseRequested += (_, settings) => saved = settings;
        viewModel.DefaultMediaFolder = missing;
        await viewModel.SaveCommand.ExecuteAsync(null);
        saved.Should().BeNull();
        viewModel.MediaFolderMessage.Should().NotBeNullOrWhiteSpace();
        var reopened = new SettingsWindowViewModel(new AppSettings { DefaultMediaFolder = missing }, new StubFolderPickerService());
        reopened.CloseRequested += (_, settings) => saved = settings;
        await reopened.SaveCommand.ExecuteAsync(null);
        saved!.DefaultMediaFolder.Should().Be(missing);
        reopened.MediaFolderMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Save_PersistsTemplatePathAndCancelDoesNotProduceSettings()
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");
        new TemplateEditorStorageService().Save(templatePath, BiographyTemplateEditorModel.CreateDefault());
        var viewModel = new SettingsWindowViewModel(
            new AppSettings { GlobalBiographyTemplatePath = $" {templatePath} " },
            new StubFolderPickerService());
        AppSettings? saved = null;
        viewModel.CloseRequested += (_, settings) => saved = settings;

        try
        {
            viewModel.SaveCommand.Execute(null);

            saved!.GlobalBiographyTemplatePath.Should().Be(templatePath);
            viewModel.CancelCommand.Execute(null);
            saved.Should().BeNull();
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Theory]
    [InlineData("mangler")]
    [InlineData("ukendt-felt")]
    public void Save_ShouldNotCloseWithMissingOrInvalidActiveTemplate(string scenario)
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");
        if (scenario == "ukendt-felt")
        {
            File.WriteAllText(templatePath, "{{ person.ukendt }}");
        }
        var viewModel = new SettingsWindowViewModel(
            new AppSettings { GlobalBiographyTemplatePath = templatePath },
            new StubFolderPickerService());
        var wasClosed = false;
        viewModel.CloseRequested += (_, _) => wasClosed = true;

        try
        {
            viewModel.SaveCommand.Execute(null);

            wasClosed.Should().BeFalse();
            viewModel.TemplateErrorMessage.Should().Contain("kan ikke gemmes som aktiv");
            viewModel.TemplateErrorMessage.Should().Contain(templatePath);
        }
        finally
        {
            if (File.Exists(templatePath))
            {
                File.Delete(templatePath);
            }
        }
    }

    [Fact]
    public void Preview_UsesTheSelectedPerson()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        var viewModel = new SettingsWindowViewModel(
            new AppSettings(),
            new StubFolderPickerService(),
            previewPerson: person);

        viewModel.PreviewBiographyTemplateCommand.Execute(null);

        viewModel.TemplateErrorMessage.Should().BeNull();
        viewModel.PreviewText.Should().Contain("# Anna Jensen");
    }

    private sealed class StubFolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
