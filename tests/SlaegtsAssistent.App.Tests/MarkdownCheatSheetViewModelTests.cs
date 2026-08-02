using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public sealed class MarkdownCheatSheetViewModelTests
{
    [Fact]
    public void Content_IsDanishAndCoversRequiredMarkdownExamples()
    {
        var viewModel = new MarkdownCheatSheetViewModel();

        viewModel.FilteredContent.Should().Contain("Overskrifter");
        viewModel.FilteredContent.Should().Contain("Tabeller");
        viewModel.FilteredContent.Should().Contain("![Beskrivelse]");
        viewModel.FilteredContent.Should().Contain("**Fed tekst**");
        viewModel.PreviewHtml.Should().Contain("<h2");
        viewModel.PreviewHtml.Should().Contain("<table>");
    }

    [Fact]
    public void Search_ReturnsMatchingLinesAndShowsNoMatchMessage()
    {
        var viewModel = new MarkdownCheatSheetViewModel
        {
            SearchText = "Tabeller",
        };

        viewModel.FilteredContent.Should().Contain("Tabeller");
        viewModel.FilteredContent.Should().NotContain("Overskrifter");

        viewModel.SearchText = "findes ikke";
        viewModel.FilteredContent.Should().Be("Ingen match fundet.");
        viewModel.PreviewHtml.Should().Contain("Ingen match fundet.");
    }
}
