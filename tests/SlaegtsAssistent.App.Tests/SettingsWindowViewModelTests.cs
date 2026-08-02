using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Tests;

public sealed class SettingsWindowViewModelTests
{
    [Fact]
    public void Save_PersistsTemplatePathAndCancelDoesNotProduceSettings()
    {
        var viewModel = new SettingsWindowViewModel(
            new AppSettings { GlobalBiographyTemplatePath = " gammel.md " },
            new StubFolderPickerService());
        AppSettings? saved = null;
        viewModel.CloseRequested += (_, settings) => saved = settings;

        viewModel.SaveCommand.Execute(null);

        saved!.GlobalBiographyTemplatePath.Should().Be("gammel.md");
        viewModel.CancelCommand.Execute(null);
        saved.Should().BeNull();
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
