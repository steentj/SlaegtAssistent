using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class GedcomDifferenceReviewViewModelTests
{
    [Fact]
    public void BulkChoices_ShouldBeReversibleAndUpdatePreviewWithoutApplying()
    {
        var document = new BiographyDocument(null, "Fri tekst", false);
        IReadOnlyDictionary<string, bool>? previewChoices = null;
        var differences = new[]
        {
            Item("person.birthDate"),
            Item("person.birthPlace"),
        };
        var viewModel = new GedcomDifferenceReviewViewModel(differences);

        viewModel.KeepAllDocumentValues();
        viewModel.SetChoice("person.birthDate", true);
        var preview = viewModel.PreviewContent;

        preview.Should().Be("preview");
        previewChoices.Should().BeEquivalentTo(new Dictionary<string, bool>
        {
            ["person.birthDate"] = true,
            ["person.birthPlace"] = false,
        });
        viewModel.UseAllImported();
        viewModel.KeepAllDocumentValues();
        viewModel.CreateDecision().Values.Should().OnlyContain(value => !value);

        GedcomDifferenceReviewItem Item(string path) => new(
            path,
            "Anna",
            "/test/anna.md",
            document,
            new BiographyFactsSnapshot(null, null, null, null, null, null, []),
            new BiographyDifference(path, null, "ny"),
            true)
        {
            CandidatePreviewFactory = choices =>
            {
                previewChoices = new Dictionary<string, bool>(choices);
                return "preview";
            },
        };
    }
}
