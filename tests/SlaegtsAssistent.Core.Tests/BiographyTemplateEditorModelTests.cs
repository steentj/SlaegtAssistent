using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyTemplateEditorModelTests
{
    [Fact]
    public void CreateDefault_ShouldRepresentTheEntireBuiltInTemplate()
    {
        var model = BiographyTemplateEditorModel.CreateDefault();

        model.Version.Should().Be(BiographyTemplateEditorModel.CurrentVersion);
        model.Blocks.Select(block => block.Type).Should().Equal(
            BiographyTemplateBlockType.Title,
            BiographyTemplateBlockType.Facts,
            BiographyTemplateBlockType.Notes,
            BiographyTemplateBlockType.Events,
            BiographyTemplateBlockType.Census,
            BiographyTemplateBlockType.Sources,
            BiographyTemplateBlockType.Media,
            BiographyTemplateBlockType.Submitter,
            BiographyTemplateBlockType.Biography);
        model.Blocks.Should().OnlyContain(block => block.IsVisible);

        var source = new BiographyTemplateEditorCompiler().Compile(model);

        source.Should().Be(ExpectedDefaultTemplate);
        BiographyTemplateMarkdownGenerator.DefaultTemplate.Should().Be(ExpectedDefaultTemplate);
        var parsed = new BiographyTemplateLoader().Parse(source, "standard.slaegtskabelon");
        parsed.ContractVersion.Should().Be(BiographyTemplateContract.CurrentVersion);
    }

    [Fact]
    public void Compile_ShouldRespectHiddenBlocksAndBlockOrder()
    {
        var standard = BiographyTemplateEditorModel.CreateDefault();
        var biography = standard.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Biography);
        var title = standard.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Title);
        var facts = standard.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
        var model = new BiographyTemplateEditorModel(
            BiographyTemplateEditorModel.CurrentVersion,
            [biography, title, facts with { IsVisible = false }]);

        var source = new BiographyTemplateEditorCompiler().Compile(model);

        source.Should().StartWith("## Biografi");
        source.Should().Contain("# {{ person.fullName }}");
        source.Should().NotContain("## Fakta");
    }

    [Fact]
    public void Compile_ShouldUseSelectedFieldsAndTablePresentation()
    {
        var model = new BiographyTemplateEditorModel(
            BiographyTemplateEditorModel.CurrentVersion,
            [
                new BiographyTemplateBlock(
                    BiographyTemplateBlockType.Events,
                    true,
                    "Livsbegivenheder",
                    BiographyTemplatePresentation.Table,
                    [BiographyTemplateField.EventDate, BiographyTemplateField.EventPlace]),
            ]);

        var source = new BiographyTemplateEditorCompiler().Compile(model);
        var template = new BiographyTemplateLoader().Parse(source);
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        person.Events.Add(new GedcomEvent("BIRT")
        {
            Category = GedcomEventCategory.Birth,
            Date = "1 JAN 1900",
            Place = "Aarhus",
        });

        var rendered = new BiographyTemplateRenderer().Render(
            template,
            BiographyTemplateContext.FromPerson(person));

        rendered.Should().Contain("## Livsbegivenheder");
        rendered.Should().Contain("| Dato | Sted |");
        rendered.Should().Contain("| 1 JAN 1900 | Aarhus |");
        rendered.Should().NotContain("Kategori");
    }

    [Fact]
    public void AeldreFaktatabel_FlytterNotaterOgBevarerFaktaraekkefoelgen()
    {
        var model = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion,
        [
            new(BiographyTemplateBlockType.Facts, true, "Fakta", BiographyTemplatePresentation.Table,
                [BiographyTemplateField.PersonNote, BiographyTemplateField.Parents, BiographyTemplateField.Birth,
                 BiographyTemplateField.Death, BiographyTemplateField.FamilyNote]),
        ]);
        var serializer = new BiographyTemplateEditorSerializer();
        var loaded = serializer.Deserialize(serializer.Serialize(model));
        var template = new BiographyTemplateLoader().Parse(new BiographyTemplateEditorCompiler().Compile(loaded));
        var person = new Person("@I1@") { BirthDate = "1900", BirthPlace = "Odense | Fyn" };
        person.Parents.Add(new Person("@I2@") { FullName = "Anna\nJensen" });
        person.Notes.Add("Første\n## Anden | linje");
        person.Notes.Add(" ");
        var family = new Family("@F1@");
        family.Notes.Add("Familienotat");
        person.Families.Add(family);
        var links = new BiographyPersonLinks(new Dictionary<string, string>
        {
            ["@I1@"] = "/tmp/barn.md",
            ["@I2@"] = "/tmp/anna.md",
        });

        var result = new BiographyTemplateRenderer().Render(template, BiographyTemplateContext.FromPerson(person, personLinks: links));

        result.Should().Contain("| Oplysning | Værdi |")
            .And.Contain("|  | Person: Første \\## Anden \\| linje |")
            .And.Contain("| Forældre | [Anna Jensen](anna.md) |")
            .And.Contain("| Født | 1900 i Odense \\| Fyn |")
            .And.Contain("|  | Familie: Familienotat |")
            .And.NotContain("| Død |");
        result.IndexOf("| Forældre |", StringComparison.Ordinal).Should().BeLessThan(result.IndexOf("| Født |", StringComparison.Ordinal));
        result.Split('\n').Count(line => line.StartsWith("|  | Person:", StringComparison.Ordinal)).Should().Be(1);
    }

    [Theory]
    [InlineData(true, "PersonNote")]
    [InlineData(false, "FamilyNote")]
    public void AeldreSkabelon_FlytterKunValgteNotaterOgBevarerSynlighed(bool visible, string selected)
    {
        var old = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion,
        [
            new(BiographyTemplateBlockType.Facts, visible, "Mine fakta", BiographyTemplatePresentation.Table,
                [BiographyTemplateField.Birth, Enum.Parse<BiographyTemplateField>(selected)]),
        ]);
        var serializer = new BiographyTemplateEditorSerializer();
        var migrated = serializer.Deserialize(serializer.Serialize(old));
        var facts = migrated.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
        var notes = migrated.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Notes);

        facts.Fields.Should().Equal(BiographyTemplateField.Birth);
        facts.Heading.Should().Be("Mine fakta");
        notes.Fields.Should().Equal(Enum.Parse<BiographyTemplateField>(selected));
        notes.IsVisible.Should().Be(visible);
        notes.Presentation.Should().Be(BiographyTemplatePresentation.Table);
        serializer.Deserialize(serializer.Serialize(migrated)).Should().BeEquivalentTo(migrated);
    }

    [Theory]
    [InlineData(BiographyTemplatePresentation.Standard)]
    [InlineData(BiographyTemplatePresentation.List)]
    [InlineData(BiographyTemplatePresentation.Table)]
    public void MedieoversigtViserTekstUdenAutomatiskeBilleder(BiographyTemplatePresentation presentation)
    {
        var block = new BiographyTemplateBlock(BiographyTemplateBlockType.Media, true, "Medier", presentation,
            [BiographyTemplateField.MediaTitle, BiographyTemplateField.MediaRelativeFile]);
        var model = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion, [block]);
        var person = new Person("@I1@");
        person.Media.Add(new Media("@M1@") { Title = "Familie | foto", File = "foto.jpg" });
        var template = new BiographyTemplateLoader().Parse(new BiographyTemplateEditorCompiler().Compile(model));

        var result = new BiographyTemplateRenderer().Render(template, BiographyTemplateContext.FromPerson(person));

        result.Should().Contain("Familie \\| foto").And.Contain("foto.jpg").And.NotContain("![");
        if (presentation == BiographyTemplatePresentation.Table)
            result.Should().Contain("| Titel | Lokal fil |");
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldPreserveTheModelDeterministically()
    {
        var standard = BiographyTemplateEditorModel.CreateDefault();
        var moved = standard.Blocks.Reverse().Select(block =>
            block.Type == BiographyTemplateBlockType.Media
                ? block with { IsVisible = false }
                : block).ToArray();
        var model = new BiographyTemplateEditorModel(
            BiographyTemplateEditorModel.CurrentVersion,
            moved);
        var serializer = new BiographyTemplateEditorSerializer();

        var first = serializer.Serialize(model);
        var restored = serializer.Deserialize(first);
        var second = serializer.Serialize(restored);

        second.Should().Be(first);
        restored.Should().BeEquivalentTo(model, options => options.WithStrictOrdering());
    }

    [Fact]
    public void SaveAndLoad_ShouldReopenTheSameModel()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.slaegtskabelon");
        var serializer = new BiographyTemplateEditorSerializer();
        var model = BiographyTemplateEditorModel.CreateDefault();

        try
        {
            serializer.Save(path, model);

            var restored = serializer.Load(path);

            restored.Should().BeEquivalentTo(model, options => options.WithStrictOrdering());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Deserialize_ShouldRejectUnknownVersionInDanish()
    {
        const string json = """
            {
              "version": 99,
              "blocks": []
            }
            """;

        var action = () => new BiographyTemplateEditorSerializer().Deserialize(json);

        action.Should().Throw<BiographyTemplateEditorException>()
            .WithMessage("*version 99*understøttes ikke*");
    }

    [Fact]
    public void Generate_ShouldBeByteIdenticalForTheSameModelAndPerson()
    {
        var model = BiographyTemplateEditorModel.CreateDefault();
        var source = new BiographyTemplateEditorCompiler().Compile(model);
        var person = new Person("@I1@")
        {
            FullName = "Anna Jensen",
            BirthDate = "1 JAN 1900",
            BirthPlace = "Aarhus",
        };
        var generator = new BiographyTemplateMarkdownGenerator(source);

        var first = generator.Generate(person);
        var second = generator.Generate(person);

        second.Should().Be(first);
    }

    [Fact]
    public void Catalog_ShouldExposeDanishLabelsSeparatelyFromTechnicalPaths()
    {
        var model = BiographyTemplateEditorModel.CreateDefault();

        foreach (var block in model.Blocks)
        {
            var descriptor = BiographyTemplateEditorCatalog.GetBlock(block.Type);
            descriptor.Label.Should().NotBeNullOrWhiteSpace();

            foreach (var field in block.Fields)
            {
                var fieldDescriptor = BiographyTemplateEditorCatalog.GetField(field);
                fieldDescriptor.Label.Should().NotBeNullOrWhiteSpace();
                fieldDescriptor.TechnicalPaths.Should().NotBeEmpty();
            }
        }
    }

    private const string ExpectedDefaultTemplate =
        "# {{ person.fullName }}\n\n" +
        "## Fakta\n" +
        "{{#if person.birthDate }}- **Født:** {{ person.birthDate }}{{#if person.birthPlace }} i {{ person.birthPlace }}{{/if}}\n{{/if}}" +
        "{{#if person.deathDate }}- **Død:** {{ person.deathDate }}{{#if person.deathPlace }} i {{ person.deathPlace }}{{/if}}\n{{/if}}" +
        "{{#if person.parents }}- **Forældre:** {{ person.parentNames }}\n{{/if}}" +
        "{{#if person.children }}- **Børn:** {{ person.childNames }}\n{{/if}}" +
        "{{#if person.partners }}- **Partnere:** {{ person.partnerNames }}\n{{/if}}" +
        "{{#if person.createdDate }}- **Oprettet dato:** {{ person.createdDate }}\n{{/if}}" +
        "\n## Notater\n" +
        "{{#each person.sectionNotes }}{{#if text }}- {{#if date }}**{{ date }}**: {{/if}}Person: {{ text }}\n{{/if}}{{/each}}" +
        "{{#each person.familySectionNotes }}{{#if text }}- {{#if date }}**{{ date }}**: {{/if}}Familie: {{ text }}\n{{/if}}{{/each}}\n" +
        "## Hændelser\n" +
        "{{#each allEvents }}- **{{ category }}**{{#if type }} ({{ type }}){{/if}}{{#if value }}: {{ value }}{{/if}}{{#if date }} ({{ date }}){{/if}}{{#if place }} i {{ place }}{{/if}}{{#if note }} – **Notat:** {{ note }}{{/if}}\n{{/each}}\n" +
        "## Folketællinger\n" +
        "{{#each census }}- {{ date }}{{#if place }} i {{ place }}{{/if}}{{#if note }}: {{ note }}{{/if}}\n{{/each}}\n" +
        "## Kilder\n" +
        "{{#each sources }}- {{ title }}{{#if author }} ({{ author }}){{/if}}{{#if page }}, side {{ page }}{{/if}}{{#if note }} – **Notat:** {{ note }}{{/if}}\n{{/each}}\n" +
        "## Medier\n" +
        "{{#each media }}- {{ title }} {{ file }}{{#if diagnostic }} ({{ diagnostic }}){{/if}}{{#if note }} – **Notat:** {{ note }}{{/if}}\n{{/each}}\n" +
        "{{#if submitter}}---\nKilde/afsender: {{ submitter.name }}\n{{/if}}" +
        "## Biografi\n\n_Skriv den fulde livshistorie her._\n";
}
