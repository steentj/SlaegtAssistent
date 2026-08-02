using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Tests;

public sealed class Sprint04dFeature04Tests
{
    [Fact]
    public void TemplateIdentity_ChangesWhenTemplateSourceChanges()
    {
        BiographyTemplateIdentity.ComputeHash("A")
            .Should()
            .NotBe(BiographyTemplateIdentity.ComputeHash("B"));
    }

    [Fact]
    public void TemplateGenerator_StoresTemplateHashAndLeavesBiographyOutsideGeneratedMarkers()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };

        var document = BiographyDocumentParser.Parse(
            new BiographyTemplateMarkdownGenerator().Generate(person));

        document.Metadata!.TemplateHash.Should().NotBeNullOrWhiteSpace();
        document.Body.IndexOf(BiographyGeneratedSectionMerger.StartMarker)
            .Should()
            .BeLessThan(document.Body.IndexOf(BiographyGeneratedSectionMerger.EndMarker));
        document.Body[(document.Body.IndexOf(BiographyGeneratedSectionMerger.EndMarker)
            + BiographyGeneratedSectionMerger.EndMarker.Length)..]
            .Should()
            .Contain("## Biografi");
    }

    [Fact]
    public void GeneratedSectionMerger_PreservesExistingBiographyWhenMigratingMarkers()
    {
        var existing = BiographyGeneratedSectionMerger.Wrap(
                "# Anna Jensen\n\n## Fakta\nGammel fakta.\n") +
            "## Biografi\n\nMin egen tekst.\n";
        var generated = BiographyGeneratedSectionMerger.Wrap(
                "# Anna Jensen\n\n## Fakta\nNy fakta.\n") +
            "## Biografi\n\n_Placeret tekst._\n";

        var candidate = BiographyGeneratedSectionMerger.CreateCandidate(existing, generated);

        candidate.Content.Should().Contain("Ny fakta.");
        candidate.Content.Should().Contain("Min egen tekst.");
        candidate.Content.Should().NotContain("_Placeret tekst._");
        candidate.RequiresMigration.Should().BeFalse();
    }
}
