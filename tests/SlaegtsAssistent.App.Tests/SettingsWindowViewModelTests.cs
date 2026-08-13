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
        var templatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");
        File.WriteAllText(templatePath, "# {{ person.fullName }}\n");
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
