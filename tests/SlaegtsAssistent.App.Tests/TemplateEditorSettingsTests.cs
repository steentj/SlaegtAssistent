using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateEditorSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly TemplateEditorStorageService _storage = new();
    private readonly EditorDialog _editor = new();
    private readonly Picker _picker = new();
    private readonly AppSettings _settings;
    private readonly string _path;

    public TemplateEditorSettingsTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "skabelon.json");
        _storage.Save(_path, BiographyTemplateEditorModel.CreateDefault());
        _settings = new AppSettings { GlobalBiographyTemplatePath = _path };
    }

    [Fact]
    public async Task CancelAfterEditing_ShouldLeaveFileAndSettingsUntouched()
    {
        var original = File.ReadAllBytes(_path);
        var viewModel = CreateViewModel();
        _editor.Result = ChangedModel();

        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);
        viewModel.CancelCommand.Execute(null);

        File.ReadAllBytes(_path).Should().Equal(original);
        _settings.GlobalBiographyTemplatePath.Should().Be(_path);
        Directory.GetFiles(_directory).Should().Equal(_path);
    }

    [Fact]
    public async Task Save_ShouldCommitDraftAndReturnValidatedPath()
    {
        var viewModel = CreateViewModel();
        _editor.Result = ChangedModel();
        AppSettings? saved = null;
        viewModel.CloseRequested += (_, settings) => saved = settings;

        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        saved!.GlobalBiographyTemplatePath.Should().Be(_path);
        _storage.Load(_path).Should().BeEquivalentTo(ChangedModel(), options => options.WithStrictOrdering());
        viewModel.TemplateErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ReopenEditor_ShouldReuseUnsavedDraftAndCancelShouldKeepIt()
    {
        var viewModel = CreateViewModel();
        _editor.Result = ChangedModel();
        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);
        _editor.Result = null;

        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        _editor.LastModel.Should().BeEquivalentTo(ChangedModel());
        _storage.Load(_path).Should().BeEquivalentTo(ChangedModel());
    }

    [Fact]
    public async Task SaveAs_ShouldKeepOriginalAndActivateNewPath()
    {
        var original = File.ReadAllBytes(_path);
        var viewModel = CreateViewModel();
        _editor.Result = ChangedModel();
        _picker.SavePath = Path.Combine(_directory, "ny-skabelon.json");
        AppSettings? saved = null;
        viewModel.CloseRequested += (_, settings) => saved = settings;

        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);
        await viewModel.SaveAsCommand.ExecuteAsync(null);

        saved!.GlobalBiographyTemplatePath.Should().Be(_picker.SavePath);
        File.ReadAllBytes(_path).Should().Equal(original);
        _storage.Load(_picker.SavePath).Should().BeEquivalentTo(ChangedModel());
    }

    [Fact]
    public async Task CancelSavePicker_ShouldKeepDraftAndNotWriteOrClose()
    {
        var original = File.ReadAllBytes(_path);
        var viewModel = CreateViewModel();
        _editor.Result = ChangedModel();
        var closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;
        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        closed.Should().BeFalse();
        File.ReadAllBytes(_path).Should().Equal(original);
        await viewModel.SaveCommand.ExecuteAsync(null);
        _storage.Load(_path).Should().BeEquivalentTo(ChangedModel());
    }

    [Fact]
    public async Task AccessFailure_ShouldKeepPreviousFilePathAndDialogOpen()
    {
        var original = File.ReadAllBytes(_path);
        var viewModel = CreateViewModel(new TemplateEditorStorageService(new AtomicFileWriter(_ =>
            throw new UnauthorizedAccessException("Ingen skriveadgang"))));
        _editor.Result = ChangedModel();
        var closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;
        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);

        await viewModel.SaveCommand.ExecuteAsync(null);

        closed.Should().BeFalse();
        viewModel.TemplateErrorMessage.Should().Contain("tidligere aktive skabelon er bevaret");
        viewModel.GlobalBiographyTemplatePath.Should().Be(_path);
        _settings.GlobalBiographyTemplatePath.Should().Be(_path);
        File.ReadAllBytes(_path).Should().Equal(original);
    }

    [Fact]
    public async Task LegacyFile_ShouldExplainTransitionAndStartNewWithoutOverwritingIt()
    {
        File.WriteAllText(_path, "# {{ person.fullName }}");
        var viewModel = CreateViewModel();
        viewModel.TemplateErrorMessage.Should().Contain("tidligere skabelonformat");
        _editor.Result = BiographyTemplateEditorModel.CreateDefault();
        _picker.SavePath = Path.Combine(_directory, "ny.json");

        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        _editor.LastModel.Should().BeEquivalentTo(BiographyTemplateEditorModel.CreateDefault());
        File.ReadAllText(_path).Should().Be("# {{ person.fullName }}");
        viewModel.GlobalBiographyTemplatePath.Should().Be(_picker.SavePath);
        _storage.Load(_picker.SavePath).Should().BeEquivalentTo(BiographyTemplateEditorModel.CreateDefault());
    }

    [Fact]
    public async Task NewTemplate_ShouldUsePickerAndReopenSavedModel()
    {
        var viewModel = CreateViewModel();
        viewModel.GlobalBiographyTemplatePath = null;
        _editor.Result = ChangedModel();
        _picker.SavePath = Path.Combine(_directory, "ny.json");
        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);

        await viewModel.SaveCommand.ExecuteAsync(null);
        _editor.Result = null;
        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);

        _editor.LastModel.Should().BeEquivalentTo(ChangedModel(), options => options.WithStrictOrdering());
    }

    private SettingsWindowViewModel CreateViewModel(TemplateEditorStorageService? storage = null) =>
        new(_settings, _picker, _picker, templateEditorDialogService: _editor, templateStorage: storage);

    [Fact]
    public async Task Editor_ShouldReceiveAllLoadedPeopleWithoutPersistingThem()
    {
        var people = new[] { new Person("@I1@") { FullName = "Anna" }, new Person("@I2@") { FullName = "Bo" } };
        var viewModel = new SettingsWindowViewModel(_settings, _picker, _picker,
            templateEditorDialogService: _editor, previewPeople: people);

        await viewModel.EditBiographyTemplateCommand.ExecuteAsync(null);

        _editor.LastPeople.Should().Equal(people);
        File.ReadAllText(_path).Should().NotContain("Anna").And.NotContain("Bo");
    }

    private static BiographyTemplateEditorModel ChangedModel()
    {
        var model = BiographyTemplateEditorModel.CreateDefault();
        return new(model.Version, model.Blocks.Reverse().Select(block =>
            block.Type == BiographyTemplateBlockType.Facts ? block with { Heading = "Mine fakta" } : block).ToArray());
    }

    public void Dispose() => Directory.Delete(_directory, true);

    private sealed class EditorDialog : ITemplateEditorDialogService
    {
        public BiographyTemplateEditorModel? Result { get; set; }
        public BiographyTemplateEditorModel? LastModel { get; private set; }
        public IReadOnlyList<Person> LastPeople { get; private set; } = [];

        public Task<BiographyTemplateEditorModel?> EditAsync(BiographyTemplateEditorModel model, IEnumerable<Person> previewPeople)
        {
            LastModel = model;
            LastPeople = previewPeople.ToArray();
            return Task.FromResult(Result);
        }
    }

    private sealed class Picker : IFolderPickerService, ITemplateFilePickerService
    {
        public string? SavePath { get; set; }
        public Task<string?> PickTemplateSaveFileAsync(string? suggestedPath) => Task.FromResult(SavePath);
        public Task<string?> PickTemplateFileAsync(string? suggestedStartFolder) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder) => Task.FromResult<string?>(null);
    }
}
