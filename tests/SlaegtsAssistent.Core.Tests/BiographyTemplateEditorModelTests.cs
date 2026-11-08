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
        "{{#if person.createdDate }}- **Oprettet dato:** {{ person.createdDate }}\n{{/if}}\n" +
        "## Hændelser\n" +
        "{{#each allEvents }}- **{{ category }}**{{#if type }} ({{ type }}){{/if}}{{#if value }}: {{ value }}{{/if}}{{#if date }} ({{ date }}){{/if}}{{#if place }} i {{ place }}{{/if}}\n{{/each}}\n" +
        "## Folketællinger\n" +
        "{{#each census }}- {{ date }}{{#if place }} i {{ place }}{{/if}}{{#if note }}: {{ note }}{{/if}}\n{{/each}}\n" +
        "## Kilder\n" +
        "{{#each sources }}- {{ title }}{{#if author }} ({{ author }}){{/if}}{{#if page }}, side {{ page }}{{/if}}\n{{/each}}\n" +
        "## Medier\n" +
        "{{#each media }}{{#if relativeFile }}![{{ title }}]({{ relativeFile }})\n{{/if}}{{/each}}\n" +
        "{{#if submitter}}---\nKilde/afsender: {{ submitter.name }}\n{{/if}}" +
        "## Biografi\n\n_Skriv den fulde livshistorie her._\n";
}
