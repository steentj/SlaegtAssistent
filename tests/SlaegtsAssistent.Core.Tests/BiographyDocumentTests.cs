using FluentAssertions;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyDocumentTests
{
    [Fact]
    public void SerializeAndParse_RoundTripsMetadataAndBody()
    {
        var metadata = new BiographyDocumentMetadata(
            1,
            "@I1@",
            "Anna Jensen",
            new BiographyFactsSnapshot(
                "Anna Jensen",
                "F",
                "12 MAR 1900",
                "Aarhus",
                null,
                null,
                ["@I2@", "@I3@"]));

        var body = "# Anna Jensen\n\nFri biografitekst.\n";
        var document = BiographyDocumentParser.Parse(
            BiographyDocumentSerializer.Serialize(metadata, body));

        document.Metadata.Should().BeEquivalentTo(metadata);
        document.Body.Should().Be(body);
        document.HasFrontMatter.Should().BeTrue();
    }

    [Fact]
    public void Parse_LegacyMarkdown_ReturnsBodyWithoutMetadata()
    {
        var body = "# Anna Jensen\r\n\r\nÆldre tekst.\r\n";

        var document = BiographyDocumentParser.Parse(body);

        document.Metadata.Should().BeNull();
        document.Body.Should().Be(body);
        document.HasFrontMatter.Should().BeFalse();
    }

    [Fact]
    public void DifferenceService_ReturnsStableFieldDifferences()
    {
        var documentFacts = new BiographyFactsSnapshot(
            "Anna Jensen", "F", "12 MAR 1900", "Aarhus", null, null, ["@I2@"]);
        var gedcomFacts = new BiographyFactsSnapshot(
            "Anna Jensen", "F", "13 MAR 1900", "Aarhus", null, null, ["@I2@", "@I3@"]);

        var differences = new BiographyDifferenceService().Compare(documentFacts, gedcomFacts);

        differences.Select(difference => difference.FieldName)
            .Should()
            .Equal("Fødselsdato", "Forældre");
    }

    [Fact]
    public void Parse_UsesVisibleBirthFactWhenItDiffersFromFrontMatter()
    {
        var metadata = new BiographyDocumentMetadata(
            1,
            "@I1@",
            "Anna Jensen",
            new BiographyFactsSnapshot("Anna Jensen", "F", "12 MAR 1900", "Aarhus", null, null, []));
        var content = BiographyDocumentSerializer.Serialize(
            metadata,
            "# Anna Jensen\n\n## Fakta\n- **Født:** 13 MAR 1900 i Aarhus\n\n## Biografi\nTekst.\n");

        var document = BiographyDocumentParser.Parse(content);

        document.Metadata!.Facts.BirthDate.Should().Be("13 MAR 1900");
        document.Metadata.Facts.BirthPlace.Should().Be("Aarhus");
    }

    [Fact]
    public void DifferenceService_DoesNotTreatUnrepresentedLegacyFieldsAsDifferences()
    {
        var legacyFacts = BiographyDocumentParser.ExtractVisibleFacts(
            "# Anna Jensen\n\n## Fakta\n- **Født:** 12 MAR 1900 i Aarhus\n",
            new BiographyFactsSnapshot(null, null, null, null, null, null, [])
            {
                RepresentedFields = new HashSet<string>(StringComparer.Ordinal),
            });
        var gedcomFacts = new BiographyFactsSnapshot(
            "Anna Jensen",
            "F",
            "12 MAR 1900",
            "Aarhus",
            null,
            null,
            ["@I2@"]);

        var differences = new BiographyDifferenceService().Compare(legacyFacts, gedcomFacts);

        differences.Should().BeEmpty();
    }

    [Fact]
    public void Updater_ChangesSelectedMetadataAndVisibleFactButPreservesBiography()
    {
        var metadata = new BiographyDocumentMetadata(
            1,
            "@I1@",
            "Anna Jensen",
            new BiographyFactsSnapshot("Anna Jensen", "F", "12 MAR 1900", null, null, null, []));
        var document = new BiographyDocument(
            metadata,
            "# Anna Jensen\n\n## Fakta\n- **Født:** 12 MAR 1900\n\n## Biografi\nMin tekst.\n",
            true);
        var gedcomFacts = new BiographyFactsSnapshot(
            "Anna Jensen", "F", "13 MAR 1900", "Aarhus", null, null, []);

        var updated = BiographyDocumentParser.Parse(
            BiographyDocumentUpdater.ApplyGedcomChoices(
                document,
                gedcomFacts,
                new Dictionary<string, bool>
                {
                    ["Fødselsdato"] = true,
                    ["Fødested"] = true,
                }));

        updated.Body.Should().Contain("- **Født:** 13 MAR 1900 i Aarhus");
        updated.Body.Should().Contain("Min tekst.");
        updated.Metadata!.Facts.BirthDate.Should().Be("13 MAR 1900");
        updated.Metadata.Facts.BirthPlace.Should().Be("Aarhus");
    }
}
