using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateMarkdownGenerator : IBiographyMarkdownGenerator
{
    public const string DefaultTemplate =
        "# {{ person.fullName }}\n\n" +
        "## Fakta\n" +
        "{{#if person.birthDate }}- **Født:** {{ person.birthDate }}{{#if person.birthPlace }} i {{ person.birthPlace }}{{/if}}\n{{/if}}" +
        "{{#if person.deathDate }}- **Død:** {{ person.deathDate }}{{#if person.deathPlace }} i {{ person.deathPlace }}{{/if}}\n{{/if}}" +
        "{{#if person.parents }}- **Forældre:** {{#each person.parents }}{{ fullName }}{{/each}}\n{{/if}}\n" +
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

    private readonly BiographyTemplate _template;
    private readonly BiographyTemplateRenderer _renderer;
    private readonly Submitter? _submitter;
    private readonly string? _mediaBaseDirectory;
    private readonly string _templateHash;

    public BiographyTemplateMarkdownGenerator(
        string? template = null,
        Submitter? submitter = null,
        string? mediaBaseDirectory = null)
    {
        _template = new BiographyTemplateLoader().Parse(template ?? DefaultTemplate);
        _renderer = new BiographyTemplateRenderer();
        _submitter = submitter;
        _mediaBaseDirectory = mediaBaseDirectory;
        _templateHash = BiographyTemplateIdentity.ComputeHash(_template.Source);
    }

    public string Generate(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        var context = BiographyTemplateContext.FromPerson(person, _submitter, _mediaBaseDirectory);
        var body = _renderer.Render(_template, context);
        var facts = BiographyFactsSnapshot.FromPerson(person);
        var canonicalSnapshot = CanonicalBiographySnapshot.Create(person, _submitter);
        return BiographyDocumentSerializer.Serialize(
            new BiographyDocumentMetadata(
                BiographyDocumentParser.CurrentFormatVersion,
                person.RecordId,
                person.FullName,
                facts)
            {
                GedcomBaselineHash = canonicalSnapshot.ComputeFingerprint(),
                TemplateHash = _templateHash,
                SyncBaseline = BiographySyncBaseline.CreateInitial(canonicalSnapshot),
            },
            CreateDocumentBody(body));
    }

    private static string CreateDocumentBody(string body)
    {
        var biographyHeading = body.IndexOf("## Biografi", StringComparison.Ordinal);
        if (biographyHeading < 0)
        {
            return BiographyGeneratedSectionMerger.Wrap(body);
        }

        var generatedPart = body[..biographyHeading].TrimEnd('\r', '\n');
        var editablePart = body[biographyHeading..].TrimStart('\r', '\n').TrimEnd('\r', '\n');
        return BiographyGeneratedSectionMerger.Wrap(generatedPart) + editablePart + "\n";
    }
}
