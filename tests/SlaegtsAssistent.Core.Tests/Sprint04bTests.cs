using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public sealed class Sprint04bTests
{
    [Fact]
    public void Load_MapsHeaderSubmitterAndMarriageToBothSpouses()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 SUBM @U1@
            0 @U1@ SUBM
            1 NAME Slægtsforeningen
            1 ADDR Hovedgade 1
            1 EMAIL info@example.test
            0 @I1@ INDI
            1 NAME Anna /Jensen/
            0 @I2@ INDI
            1 NAME Jens /Hansen/
            0 @F1@ FAM
            1 HUSB @I2@
            1 WIFE @I1@
            1 MARR
            2 DATE 10 JUN 1920
            2 PLAC Aarhus
            2 SOUR @S1@
            0 @S1@ SOUR
            1 TITL Vielsesbog
            0 TRLR
            """);

        try
        {
            var tree = new GedcomLoader().Load(path);

            tree.SubmitterRecordId.Should().Be("@U1@");
            tree.Submitter.Should().NotBeNull();
            tree.Submitter!.Name.Should().Be("Slægtsforeningen");
            tree.Submitter.Email.Should().Be("info@example.test");

            var anna = tree.FindPerson("@I1@")!;
            var marriage = anna.FamilyEvents.Should().ContainSingle().Subject;
            marriage.Category.Should().Be(GedcomEventCategory.Marriage);
            marriage.Date.Should().Be("10 JUN 1920");
            marriage.Place.Should().Be("Aarhus");
            marriage.Sources.Should().ContainSingle(source => source.Title == "Vielsesbog");
            tree.FindPerson("@I2@")!.FamilyEvents.Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_ClassifiesConfirmationMilitaryAndUnknownEventsWithoutDroppingData()
    {
        var path = CreateFixture(
            """
            0 HEAD
            0 @I1@ INDI
            1 NAME Anna /Jensen/
            1 CONF
            2 DATE 04 MAY 1915
            1 EVEN Lægdsrulle
            2 TYPE Lægdsrulle
            2 DATE 01 JAN 1918
            2 PLAC Aarhus
            2 NOTE Værnepligtig
            1 _CUSTOM Special hændelse
            2 NOTE Ukendt tag
            0 TRLR
            """);

        try
        {
            var tree = new GedcomLoader().Load(path);
            var person = tree.FindPerson("@I1@")!;

            person.Events.Select(@event => @event.Category)
                .Should()
                .Equal(
                    GedcomEventCategory.Confirmation,
                    GedcomEventCategory.MilitaryService,
                    GedcomEventCategory.Other);
            person.Events[1].Type.Should().Be("Lægdsrulle");
            person.Events[2].Note.Should().Be("Ukendt tag");
            tree.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Tag == "_CUSTOM");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TemplateRenderer_RendersConditionalsLoopsAndRelativeMedia()
    {
        var person = new Person("@I1@")
        {
            FullName = "Anna Jensen",
        };
        person.Events.Add(new GedcomEvent("EVEN")
        {
            Category = GedcomEventCategory.Other,
            Value = "Flytning",
            Date = "1920",
        });
        person.Media.Add(new Media("@M1@") { File = @"scans\anna.jpg" });

        var template = new BiographyTemplateLoader().Parse(
            "# {{ person.fullName }}\n" +
            "{{#if person.birthDate}}Født: {{ person.birthDate }}\n{{/if}}" +
            "{{#each events}}- {{ value }} ({{ date }})\n{{/each}}" +
            "{{#each media}}![{{ title }}]({{ relativeFile }})\n{{/each}}");

        var markdown = new BiographyTemplateRenderer().Render(
            template,
            BiographyTemplateContext.FromPerson(person));

        markdown.Should().Be(
            "# Anna Jensen\n" +
            "- Flytning (1920)\n" +
            "![](scans/anna.jpg)\n");
    }

    [Fact]
    public void DefaultTemplateIncludesGeneratedMarkersAndAllElementGroups()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        person.Events.Add(new GedcomEvent("EVEN")
        {
            Category = GedcomEventCategory.MilitaryService,
            Type = "Lægdsrulle",
        });
        person.Census.Add(new Census { Date = "1911" });
        person.Sources.Add(new Source("@S1@") { Title = "Kirkebog" });
        person.Media.Add(new Media("@M1@") { File = "anna.jpg" });

        var markdown = new BiographyTemplateMarkdownGenerator(
            submitter: new Submitter("@U1@") { Name = "Arkivet" })
            .Generate(person);

        markdown.Should().Contain("## Hændelser");
        markdown.Should().Contain("Lægdsrulle");
        markdown.Should().Contain("## Folketællinger");
        markdown.Should().Contain("## Kilder");
        markdown.Should().Contain("Kirkebog");
        markdown.Should().Contain("![");
        markdown.Should().Contain("Kilde/afsender: Arkivet");
        markdown.Should().Contain(BiographyGeneratedSectionMerger.StartMarker);
    }

    [Fact]
    public void TemplateParser_ReportsFileLineAndColumnForInvalidSyntax()
    {
        var action = () => new BiographyTemplateLoader().Parse(
            "# Person\n{{#if person.fullName}}\n",
            "person.md");

        var exception = action.Should().Throw<BiographyTemplateException>().Which;
        exception.FilePath.Should().Be("person.md");
        exception.Line.Should().Be(2);
        exception.Column.Should().Be(1);
    }

    [Fact]
    public void GeneratedSectionMerger_PreservesFreeTextAndRequiresApprovalForLegacyDocument()
    {
        var existing = "# Anna\n\nFri tekst.\n";

        var candidate = BiographyGeneratedSectionMerger.CreateCandidate(existing, "## Fakta\n");

        candidate.RequiresMigration.Should().BeTrue();
        candidate.Content.Should().Contain("Fri tekst.");
        candidate.Content.Should().Contain(BiographyGeneratedSectionMerger.StartMarker);
    }

    private static string CreateFixture(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllText(path, content);
        return path;
    }
}
