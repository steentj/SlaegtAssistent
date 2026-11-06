using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyPersonLinksTests
{
    [Fact]
    public void RelativePath_EncodesSegmentsAndUsesSourceDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "personlinks");
        var links = new BiographyPersonLinks(new Dictionary<string, string>
        {
            ["@I1@"] = Path.Combine(root, "børn", "barn.md"),
            ["@I2@"] = Path.Combine(root, "Ældre slægt", "Anna (#1) 50%.md"),
        });
        links.GetRelativePath("@I1@", "@I2@").Should()
            .Be("../%C3%86ldre%20sl%C3%A6gt/Anna%20%28%231%29%2050%25.md");
    }

    [Fact]
    public void AmbiguousAndMissingTargets_DoNotCreateLinks()
    {
        var links = new BiographyPersonLinks([
            new("@I1@", "/tmp/barn.md"),
            new("@I2@", "/tmp/anna.md"),
            new("@I2@", "/tmp/kopi.md"),
        ]);
        links.GetRelativePath("@I1@", "@I2@").Should().BeNull();
        links.GetRelativePath("@I1@", "@I3@").Should().BeNull();
        var person = new Person("@I1@");
        person.Parents.Add(new Person("@I2@") { FullName = "Anna [Jensen]" });
        var markdown = new BiographyTemplateMarkdownGenerator(personLinks: links).Generate(person);
        markdown.Should().Contain("**Forældre:** Anna \\[Jensen\\]").And.NotContain("](anna");
    }

    [Fact]
    public void ReferenceFields_LinkByIdentityAndEscapeNamesWithoutChangingHeadingOrProse()
    {
        var person = new Person("@I1@") { FullName = "Barn" };
        person.Parents.Add(new Person("@I2@") { FullName = "Anna [Jensen]" });
        person.Parents.Add(new Person("@I3@") { FullName = "Anna [Jensen]" });
        var links = new BiographyPersonLinks(new Dictionary<string, string>
        {
            ["@I1@"] = "/tmp/barn.md", ["@I2@"] = "/tmp/anna1.md", ["@I3@"] = "/tmp/anna2.md",
        });
        var generator = new BiographyTemplateMarkdownGenerator(
            "# {{ person.fullName }}\n{{ person.parentNames }}\n{{#each person.parents}}{{ fullName }} | {{ link }} | {{ plainName }}\n{{/each}}\n## Biografi\nAnna [Jensen] er omtalt her.",
            personLinks: links);
        var markdown = generator.Generate(person);
        markdown.Should().Contain("# Barn\n")
            .And.Contain("[Anna \\[Jensen\\]](anna1.md), [Anna \\[Jensen\\]](anna2.md)")
            .And.Contain("[Anna \\[Jensen\\]](anna1.md) | [Anna \\[Jensen\\]](anna1.md) | Anna \\[Jensen\\]")
            .And.Contain("Anna [Jensen] er omtalt her.");
        generator.Generate(person).Should().Be(markdown);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void NameListCondition_ReflectsWhetherRelationsExist(bool hasParent, bool expected)
    {
        var person = new Person("@I1@");
        if (hasParent) person.Parents.Add(new Person("@I2@") { FullName = "Anna" });
        var markdown = new BiographyTemplateMarkdownGenerator(
            "{{#if person.parentNames}}Forældreliste{{/if}}", personLinks: new BiographyPersonLinks([])).Generate(person);
        markdown.Contains("Forældreliste", StringComparison.Ordinal).Should().Be(expected);
    }

    [Fact]
    public void SnapshotRendering_UsesApprovedRelationsForParentsChildrenAndPartners()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "0 HEAD\n1 SOUR Test\n1 GEDC\n2 VERS 5.5.1\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NAME Far\n0 @I2@ INDI\n1 NAME Mor\n0 @I3@ INDI\n1 NAME Barn\n0 @F1@ FAM\n1 HUSB @I1@\n1 WIFE @I2@\n1 CHIL @I3@\n0 TRLR\n");
            var tree = new GedcomLoader().Load(file);
            var links = new BiographyPersonLinks(tree.People.Select(person =>
                KeyValuePair.Create(person.RecordId, Path.Combine(Path.GetTempPath(), BiographyFileNameGenerator.Generate(person)))));
            var generator = new BiographyTemplateMarkdownGenerator(personLinks: links);
            var father = tree.FindPerson("@I1@")!;
            var snapshot = CanonicalBiographySnapshot.Create(father);
            var names = tree.People.ToDictionary(person => person.RecordId, person => person.FullName);
            var markdown = generator.Generate(snapshot, snapshot, names);
            markdown.Should().Contain("**Børn:** [Barn](").And.Contain("**Partnere:** [Mor](");
            generator.Generate(father).Should().Contain("**Børn:** [Barn](").And.Contain("**Partnere:** [Mor](");
            var approved = snapshot with { ChildRecordIds = [], Families = [] };
            generator.Generate(approved, snapshot, names).Should().NotContain("**Børn:**").And.NotContain("**Partnere:**");
            generator.Generate(tree.FindPerson("@I3@")!).Should().Contain("**Forældre:** [Far](");
        }
        finally { File.Delete(file); }
    }
}
