using FluentAssertions;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyTemplateExampleDataTests
{
    [Fact]
    public void Create_ShouldReturnDeterministicDanishDataForEverySupportedBlock()
    {
        var first = BiographyTemplateExampleData.Create();
        var second = BiographyTemplateExampleData.Create();

        first.Person.FullName.Should().Be("Anna Marie Jensen");
        first.Person.Parents.Should().HaveCount(2);
        first.Person.Children.Should().NotBeEmpty();
        first.Person.Families.Should().ContainSingle();
        first.Person.Events.Should().HaveCountGreaterThan(1);
        first.Person.Census.Should().NotBeEmpty();
        first.Person.Sources.Should().NotBeEmpty();
        first.Person.Media.Should().NotBeEmpty();
        first.Submitter.Name.Should().Be("Det lokale slægtsarkiv");

        var compiler = new BiographyTemplateEditorCompiler();
        var template = new BiographyTemplateLoader().Parse(
            compiler.Compile(BiographyTemplateEditorModel.CreateDefault()));
        var renderer = new BiographyTemplateRenderer();
        var firstMarkdown = renderer.Render(
            template,
            BiographyTemplateContext.FromPerson(first.Person, first.Submitter));
        var secondMarkdown = renderer.Render(
            template,
            BiographyTemplateContext.FromPerson(second.Person, second.Submitter));

        secondMarkdown.Should().Be(firstMarkdown);
        firstMarkdown.Should().Contain("## Fakta");
        firstMarkdown.Should().Contain("## Hændelser");
        firstMarkdown.Should().Contain("## Folketællinger");
        firstMarkdown.Should().Contain("## Kilder");
        firstMarkdown.Should().Contain("## Medier");
        firstMarkdown.Should().Contain("Kilde/afsender: Det lokale slægtsarkiv");
    }
}
