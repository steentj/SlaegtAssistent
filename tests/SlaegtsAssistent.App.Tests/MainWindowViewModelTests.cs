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
        var viewModel = new MainWindowViewModel();

        viewModel.People.Should().BeEmpty();
        viewModel.SelectedPerson.Should().BeNull();
    }

    [Fact]
    public void SettingSelectedPerson_ShouldRaisePropertyChanged()
    {
        var viewModel = new MainWindowViewModel();
        var selectedPerson = new PersonListItemViewModel("@I1@", "Anna Jensen");
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
        var viewModel = new MainWindowViewModel(loader, picker);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        loader.Calls.Should().Be(0);
        viewModel.People.Should().BeEmpty();
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

        var picker = new FakeGedcomFilePickerService(file.Path);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var viewModel = new MainWindowViewModel(loader, picker);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        loader.Calls.Should().Be(1);
        loader.LastPath.Should().Be(file.Path);
        viewModel.People.Select(person => person.DisplayName)
            .Should()
            .ContainInOrder("Anna Jensen", "Bo Jensen");
        viewModel.SelectedPerson?.DisplayName.Should().Be("Anna Jensen");
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.SelectedGedcomFilePath.Should().Be(file.Path);
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

        var picker = new SequencedGedcomFilePickerService([firstFile.Path, secondFile.Path]);
        var loader = new RecordingGedcomLoader(path => new GedcomLoader().Load(path));
        var viewModel = new MainWindowViewModel(loader, picker);

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
        var picker = new FakeGedcomFilePickerService("/tmp/invalid.ged");
        var loader = new ThrowingGedcomLoader(new GedcomLoadException("Filen kunne ikke laeses."));
        var viewModel = new MainWindowViewModel(loader, picker);

        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("Kunne ikke indlaese GEDCOM-fil: Filen kunne ikke laeses.");
        viewModel.People.Should().BeEmpty();
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

        public Task<string?> PickGedcomFileAsync()
        {
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

        public Task<string?> PickGedcomFileAsync()
        {
            return Task.FromResult(_paths.Dequeue());
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
