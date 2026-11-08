using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateCheatSheetViewModelTests
{
    [Fact]
    public void Content_ExplainsGuidedEditingAndSafeApplicationWithoutSyntax()
    {
        var viewModel = new TemplateCheatSheetViewModel();

        viewModel.FilteredContent.Should().Contain("Brug kladde");
        viewModel.FilteredContent.Should().Contain("Anvend skabelon");
        viewModel.FilteredContent.Should().Contain("uden at genindlæse GEDCOM");
        viewModel.FilteredContent.Should().Contain("Annuller");
        viewModel.FilteredContent.Should().NotContain("{{");
        viewModel.FilteredContent.Should().NotContain("person.");
        viewModel.PreviewHtml.Should().Contain("<h2");
    }

    [Fact]
    public void Content_ShouldIncludeHelpAndExampleForEveryBlock()
    {
        foreach (var block in BiographyTemplateEditorCatalog.Blocks)
        {
            block.Help.Should().NotBeNullOrWhiteSpace();
            block.Example.Should().NotBeNullOrWhiteSpace();
            TemplateCheatSheetViewModel.Content.Should().Contain(block.Help);
            TemplateCheatSheetViewModel.Content.Should().Contain(block.Example);
        }
    }

    [Fact]
    public void Search_FiltersTemplateReference()
    {
        var viewModel = new TemplateCheatSheetViewModel
        {
            SearchText = "afsender",
        };

        viewModel.FilteredContent.Should().Contain("Afsender");
        viewModel.FilteredContent.Should().Contain("Eksempel:");
        viewModel.FilteredContent.Should().NotContain("## Hændelser");
    }
}
