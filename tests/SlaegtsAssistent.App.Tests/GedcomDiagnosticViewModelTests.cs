using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Tests;

public sealed class GedcomDiagnosticViewModelTests
{
    [Fact]
    public void CreateSummaries_OnlyGroupsSameKindTagSeverityAndFile()
    {
        var metadata = new GedcomDiagnostic(
            GedcomDiagnosticSeverity.Warning,
            "Metadata blev sprunget over.",
            3,
            "@X1@",
            "_PTF",
            "Metadata blev ikke importeret.",
            "første.ged",
            GedcomDiagnosticKind.IgnoredMacFamilyTreeMetadata);
        var other = new GedcomDiagnostic(GedcomDiagnosticSeverity.Error, "Anden fejl.", 4, "@I1@", "INDI");
        var diagnostics = new[]
        {
            metadata,
            other,
            metadata with { Line = 5, RecordId = "@X2@" },
            metadata with { FilePath = "anden.ged" },
            metadata with { Tag = "_PLAC" },
            metadata with { Severity = GedcomDiagnosticSeverity.Error },
            metadata with { Kind = GedcomDiagnosticKind.DuplicateMacFamilyTreeMetadata },
            other with { Line = 6 },
        };

        var summaries = GedcomDiagnosticViewModel.CreateSummaries(diagnostics);

        summaries.Should().HaveCount(7);
        summaries.Select(item => item.Occurrences).Should().Equal(2, 1, 1, 1, 1, 1, 1);
        summaries[0].LocationText.Should().Be("_PTF");
        summaries[0].Message.Should().EndWith("Antal forekomster: 2.");
        summaries[0].RecordId.Should().BeNull();
        summaries[1].DialogText.Should().Be("Linje 4, INDI: Anden fejl. Konsekvens: ");
        summaries[2].LocationText.Should().Be("@X1@ · _PTF · linje 3");
        summaries[2].Message.Should().Be("Metadata blev sprunget over.");
        summaries[2].RecordId.Should().Be("@X1@");
    }

    [Fact]
    public void CreateSummaries_CountsLargeNumbersOfMetadataDiagnostics()
    {
        var diagnostics = Enumerable.Range(1, 5000).Select(index => new GedcomDiagnostic(
            GedcomDiagnosticSeverity.Warning,
            "Metadata blev sprunget over.",
            index,
            $"@X{index}@",
            "_PLAC",
            Kind: GedcomDiagnosticKind.IgnoredMacFamilyTreeMetadata));

        var summaries = GedcomDiagnosticViewModel.CreateSummaries(diagnostics);

        summaries.Should().ContainSingle();
        summaries[0].Occurrences.Should().Be(5000);
        summaries[0].DialogText.Should().Contain("Antal forekomster: 5000.");
    }
}
