using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyTemplateDocumentUpdaterTests
{
    [Fact]
    public void CreateCandidate_ShouldUseApprovedDataAndPreserveDocumentFactsAndBaseline()
    {
        var example = BiographyTemplateExampleData.Create();
        var original = BiographyDocumentParser.Parse(new BiographyTemplateMarkdownGenerator()
            .Generate(example.Person).Replace("_Skriv den fulde livshistorie her._", "Min egen tekst."));
        var metadata = original.Metadata! with
        {
            Facts = original.Metadata!.Facts with { BirthPlace = "Brugerens fødested" },
        };
        var document = original with { Metadata = metadata };
        var model = BiographyTemplateEditorModel.CreateDefault();
        var source = new BiographyTemplateEditorCompiler().Compile(new(model.Version,
            model.Blocks.Select(block => block.Type == BiographyTemplateBlockType.Facts
                ? block with { Heading = "Mine fakta" } : block).ToArray()));

        var content = new BiographyTemplateDocumentUpdater().CreateCandidate(document, source);

        var candidate = BiographyDocumentParser.Parse(content);
        candidate.Body.Should().Contain("## Mine fakta").And.Contain("Min egen tekst.")
            .And.Contain("Brugerens fødested");
        candidate.Metadata!.Facts.Should().BeEquivalentTo(metadata.Facts,
            options => options.Excluding(facts => facts.ParentDisplayText));
        candidate.Metadata.SyncBaseline.Should().BeEquivalentTo(metadata.SyncBaseline);
        candidate.Metadata.GedcomBaselineHash.Should().Be(metadata.GedcomBaselineHash);
        candidate.Metadata.TemplateHash.Should().Be(BiographyTemplateIdentity.ComputeHash(source));
    }

    [Fact]
    public void CreateCandidate_ShouldRejectMissingApprovedSnapshot()
    {
        var document = new BiographyDocument(null, "Min tekst.", false);

        var generate = () => new BiographyTemplateDocumentUpdater().CreateCandidate(
            document, BiographyTemplateMarkdownGenerator.DefaultTemplate);

        generate.Should().Throw<InvalidOperationException>().WithMessage("*godkendt*snapshot*");
    }

    [Fact]
    public void CreateCandidate_ShouldNotAddWhitespaceWhenTemplateIsUnchanged()
    {
        var original = new BiographyTemplateMarkdownGenerator().Generate(new Person("@I1@") { FullName = "Anna" });

        var candidate = new BiographyTemplateDocumentUpdater().CreateCandidate(
            BiographyDocumentParser.Parse(original), BiographyTemplateMarkdownGenerator.DefaultTemplate);

        candidate.Should().Be(original);
    }
}
