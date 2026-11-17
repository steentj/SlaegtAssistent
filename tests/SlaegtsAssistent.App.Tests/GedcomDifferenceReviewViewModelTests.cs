using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class GedcomDifferenceReviewViewModelTests
{
    [Fact]
    public void PersonSelectionAndFiltering_ShouldPreserveDecisionsAndUseSelectedPersonsPreview()
    {
        var document = new BiographyDocument(null, "Fri tekst", false);
        GedcomDifferenceReviewItem Item(string key, string person, string path, bool initial) => new(
            key, person, path, document, new BiographyFactsSnapshot(null, null, null, null, null, null, []),
            new BiographyDifference("Fødselsdato", "gammel", "ny"), initial)
        {
            CandidatePreviewFactory = choices => $"{person}: {choices[key]}",
        };
        var viewModel = new GedcomDifferenceReviewViewModel([
            Item("a1", "Anna", "/a.md", true), Item("a2", "Anna", "/a.md", false),
            Item("b1", "Bo", "/b.md", false),
        ]);
        viewModel.People.Should().HaveCount(2);
        viewModel.People[0].Differences.Should().HaveCount(2);
        viewModel.PreviewContent.Should().Be("Anna: True");
        viewModel.SetChoice("a1", false);
        viewModel.SelectedPerson = viewModel.FilterPeople("bo").Single();
        viewModel.PreviewContent.Should().Be("Bo: False");
        viewModel.SetChoice("b1", true);
        viewModel.SelectedPerson = viewModel.FilterPeople("anna").Single();
        viewModel.PreviewContent.Should().Be("Anna: False");
        viewModel.SelectedCount.Should().Be(1);
        viewModel.CreateDecision().Should().BeEquivalentTo(new Dictionary<string, bool>
        {
            ["a1"] = false, ["a2"] = false, ["b1"] = true,
        });
    }

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
