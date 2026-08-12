using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public class PersonListItemViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeDisplayName()
    {
        var viewModel = new PersonListItemViewModel("@I1@", "Anna Jensen", "/tmp/anna-jensen.md");

        viewModel.RecordId.Should().Be("@I1@");
        viewModel.DisplayName.Should().Be("Anna Jensen");
        viewModel.MarkdownFilePath.Should().Be("/tmp/anna-jensen.md");
    }

    [Fact]
    public void Constructor_WithDocumentDiagnostic_ShouldExposePathCategoryAndNextAction()
    {
        var viewModel = new PersonListItemViewModel(
            "error:defekt.md",
            "defekt.md",
            "/tmp/defekt.md",
            documentErrorCategory: "Dubleret nøgle",
            documentErrorMessage: "Nøglen recordId forekommer flere gange.",
            documentNextAction: "Ret den dublerede nøgle.");

        viewModel.HasDocumentDiagnostic.Should().BeTrue();
        viewModel.DocumentDiagnosticText.Should().Contain("Dubleret nøgle");
        viewModel.DocumentDiagnosticText.Should().Contain("/tmp/defekt.md");
        viewModel.DocumentDiagnosticText.Should().Contain("Ret den dublerede nøgle");
        viewModel.ToolTipText.Should().Be(viewModel.DocumentDiagnosticText);
    }
}
