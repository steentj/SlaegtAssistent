using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateCheatSheetViewModelTests
{
    [Fact]
    public void Content_DocumentsTheSupportedTemplateContract()
    {
        var viewModel = new TemplateCheatSheetViewModel();

        viewModel.FilteredContent.Should().Contain("person.fullName");
        viewModel.FilteredContent.Should().Contain("familyEvents");
        viewModel.FilteredContent.Should().Contain("submitter.name");
        viewModel.FilteredContent.Should().Contain("{{#each allEvents}}");
        viewModel.PreviewHtml.Should().Contain("<table");
    }

    [Fact]
    public void Search_FiltersTemplateReference()
    {
        var viewModel = new TemplateCheatSheetViewModel
        {
            SearchText = "SUBM",
        };

        viewModel.FilteredContent.Should().Contain("Afsender");
        viewModel.FilteredContent.Should().NotContain("Tabel-eksempel");
    }
}
