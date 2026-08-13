using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

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
    public void Content_ShouldMatchEveryFieldInVersionedContract()
    {
        TemplateCheatSheetViewModel.Content.Should().Contain(
            $"feltkontrakt version {BiographyTemplateContract.CurrentVersion}");
        foreach (var path in BiographyTemplateContract.PublicFieldPaths)
        {
            TemplateCheatSheetViewModel.ContractFieldList.Should().Contain($"`{path}`");
        }
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
