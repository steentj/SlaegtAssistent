using System.Text;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateEditorCompiler
{
    public string Compile(BiographyTemplateEditorModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ValidateModel(model);

        var result = new StringBuilder();
        foreach (var block in model.Blocks.Where(item => item.IsVisible))
        {
            AppendBlock(result, block);
        }

        var source = result.ToString();
        new BiographyTemplateLoader().Parse(source);
        return source;
    }

    private static void ValidateModel(BiographyTemplateEditorModel model)
    {
        if (model.Version != BiographyTemplateEditorModel.CurrentVersion)
        {
            throw new BiographyTemplateEditorException(
                $"Skabelonversion {model.Version} understøttes ikke. Den aktuelle version er {BiographyTemplateEditorModel.CurrentVersion}.");
        }

        var duplicates = model.Blocks
            .GroupBy(block => block.Type)
            .Where(group => group.Count() > 1)
            .Select(group => BiographyTemplateEditorCatalog.GetBlock(group.Key).Label)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new BiographyTemplateEditorException(
                $"Skabelonen indeholder blokken {string.Join(", ", duplicates)} mere end én gang.");
        }

        foreach (var block in model.Blocks)
        {
            ValidateHeading(block);
            ValidateFields(block);
            ValidatePresentation(block);
        }
    }

    private static void ValidateHeading(BiographyTemplateBlock block)
    {
        if (block.Heading.Contains('\n') || block.Heading.Contains('\r'))
        {
            throw new BiographyTemplateEditorException("En blokoverskrift må ikke indeholde linjeskift.");
        }

        if (block.Type is not BiographyTemplateBlockType.Title and not BiographyTemplateBlockType.Submitter &&
            string.IsNullOrWhiteSpace(block.Heading))
        {
            throw new BiographyTemplateEditorException(
                $"Blokken {BiographyTemplateEditorCatalog.GetBlock(block.Type).Label} mangler en overskrift.");
        }
    }

    private static void ValidateFields(BiographyTemplateBlock block)
    {
        var allowed = BiographyTemplateEditorCatalog.GetFields(block.Type)
            .Select(item => item.Field)
            .ToHashSet();
        var invalid = block.Fields.Where(field => !allowed.Contains(field)).ToArray();
        if (invalid.Length > 0)
        {
            throw new BiographyTemplateEditorException(
                $"Blokken {BiographyTemplateEditorCatalog.GetBlock(block.Type).Label} indeholder et felt, som ikke kan bruges i blokken.");
        }

        if (block.Fields.Distinct().Count() != block.Fields.Count)
        {
            throw new BiographyTemplateEditorException(
                $"Blokken {BiographyTemplateEditorCatalog.GetBlock(block.Type).Label} indeholder det samme felt mere end én gang.");
        }
    }

    private static void ValidatePresentation(BiographyTemplateBlock block)
    {
        var allowed = BiographyTemplateEditorCatalog.GetPresentations(block.Type)
            .Any(item => item.Presentation == block.Presentation);

        if (!allowed)
        {
            throw new BiographyTemplateEditorException(
                $"Visningen `{block.Presentation}` kan ikke bruges til blokken {BiographyTemplateEditorCatalog.GetBlock(block.Type).Label}.");
        }
    }

    private static void AppendBlock(StringBuilder result, BiographyTemplateBlock block)
    {
        switch (block.Type)
        {
            case BiographyTemplateBlockType.Title:
                result.Append("# {{ person.fullName }}\n\n");
                break;
            case BiographyTemplateBlockType.Facts:
                AppendFacts(result, block);
                break;
            case BiographyTemplateBlockType.Events:
                AppendEvents(result, block);
                break;
            case BiographyTemplateBlockType.Census:
                AppendCensus(result, block);
                break;
            case BiographyTemplateBlockType.Sources:
                AppendSources(result, block);
                break;
            case BiographyTemplateBlockType.Media:
                AppendMedia(result, block);
                break;
            case BiographyTemplateBlockType.Submitter:
                AppendSubmitter(result, block);
                break;
            case BiographyTemplateBlockType.Biography:
                result.Append("## ").Append(block.Heading).Append("\n\n_Skriv den fulde livshistorie her._\n");
                break;
            default:
                throw new BiographyTemplateEditorException($"Ukendt bloktype `{block.Type}`.");
        }
    }

    private static void AppendFacts(StringBuilder result, BiographyTemplateBlock block)
    {
        AppendHeading(result, block);
        foreach (var field in block.Fields)
        {
            result.Append(field switch
            {
                BiographyTemplateField.Birth =>
                    "{{#if person.birthDate }}- **Født:** {{ person.birthDate }}{{#if person.birthPlace }} i {{ person.birthPlace }}{{/if}}\n{{/if}}",
                BiographyTemplateField.Death =>
                    "{{#if person.deathDate }}- **Død:** {{ person.deathDate }}{{#if person.deathPlace }} i {{ person.deathPlace }}{{/if}}\n{{/if}}",
                BiographyTemplateField.Parents =>
                    "{{#if person.parents }}- **Forældre:** {{ person.parentNames }}\n{{/if}}",
                BiographyTemplateField.Children =>
                    "{{#if person.children }}- **Børn:** {{ person.childNames }}\n{{/if}}",
                BiographyTemplateField.Partners =>
                    "{{#if person.partners }}- **Partnere:** {{ person.partnerNames }}\n{{/if}}",
                BiographyTemplateField.CreatedDate =>
                    "{{#if person.createdDate }}- **Oprettet dato:** {{ person.createdDate }}\n{{/if}}",
                _ => throw UnexpectedField(block, field),
            });
        }
        result.Append('\n');
    }

    private static void AppendEvents(StringBuilder result, BiographyTemplateBlock block)
    {
        AppendHeading(result, block);
        if (block.Presentation == BiographyTemplatePresentation.Table)
        {
            AppendTable(result, "allEvents", block.Fields, EventColumns);
            return;
        }

        result.Append("{{#each allEvents }}-");
        foreach (var field in block.Fields)
        {
            result.Append(field switch
            {
                BiographyTemplateField.EventCategory => " **{{ category }}**",
                BiographyTemplateField.EventType => "{{#if type }} ({{ type }}){{/if}}",
                BiographyTemplateField.EventValue => "{{#if value }}: {{ value }}{{/if}}",
                BiographyTemplateField.EventDate => "{{#if date }} ({{ date }}){{/if}}",
                BiographyTemplateField.EventPlace => "{{#if place }} i {{ place }}{{/if}}",
                _ => throw UnexpectedField(block, field),
            });
        }
        result.Append("\n{{/each}}\n");
    }

    private static void AppendCensus(StringBuilder result, BiographyTemplateBlock block)
    {
        AppendHeading(result, block);
        if (block.Presentation == BiographyTemplatePresentation.Table)
        {
            AppendTable(result, "census", block.Fields, CensusColumns);
            return;
        }

        result.Append("{{#each census }}-");
        foreach (var field in block.Fields)
        {
            result.Append(field switch
            {
                BiographyTemplateField.CensusDate => " {{ date }}",
                BiographyTemplateField.CensusPlace => "{{#if place }} i {{ place }}{{/if}}",
                BiographyTemplateField.CensusNote => "{{#if note }}: {{ note }}{{/if}}",
                _ => throw UnexpectedField(block, field),
            });
        }
        result.Append("\n{{/each}}\n");
    }

    private static void AppendSources(StringBuilder result, BiographyTemplateBlock block)
    {
        AppendHeading(result, block);
        if (block.Presentation == BiographyTemplatePresentation.Table)
        {
            AppendTable(result, "sources", block.Fields, SourceColumns);
            return;
        }

        result.Append("{{#each sources }}-");
        foreach (var field in block.Fields)
        {
            result.Append(field switch
            {
                BiographyTemplateField.SourceTitle => " {{ title }}",
                BiographyTemplateField.SourceAuthor => "{{#if author }} ({{ author }}){{/if}}",
                BiographyTemplateField.SourcePage => "{{#if page }}, side {{ page }}{{/if}}",
                _ => throw UnexpectedField(block, field),
            });
        }
        result.Append("\n{{/each}}\n");
    }

    private static void AppendMedia(StringBuilder result, BiographyTemplateBlock block)
    {
        AppendHeading(result, block);
        var title = block.Fields.Contains(BiographyTemplateField.MediaTitle) ? "{{ title }}" : string.Empty;
        if (block.Fields.Contains(BiographyTemplateField.MediaRelativeFile))
        {
            result.Append("{{#each media }}{{#if relativeFile }}![")
                .Append(title)
                .Append("]({{ relativeFile }})\n{{/if}}{{/each}}\n");
        }
        else if (block.Fields.Contains(BiographyTemplateField.MediaTitle))
        {
            result.Append("{{#each media }}- {{ title }}\n{{/each}}\n");
        }
        else
        {
            result.Append('\n');
        }
    }

    private static void AppendSubmitter(StringBuilder result, BiographyTemplateBlock block)
    {
        if (block.Fields.Contains(BiographyTemplateField.SubmitterName))
        {
            result.Append("{{#if submitter}}---\nKilde/afsender: {{ submitter.name }}\n{{/if}}");
        }
    }

    private static void AppendHeading(StringBuilder result, BiographyTemplateBlock block) =>
        result.Append("## ").Append(block.Heading).Append('\n');

    private static void AppendTable(
        StringBuilder result,
        string collection,
        IReadOnlyList<BiographyTemplateField> fields,
        IReadOnlyDictionary<BiographyTemplateField, TableColumn> columns)
    {
        if (fields.Count == 0)
        {
            result.Append('\n');
            return;
        }

        result.Append("| ").AppendJoin(" | ", fields.Select(field => columns[field].Heading)).Append(" |\n");
        result.Append("| ").AppendJoin(" | ", fields.Select(_ => "---")).Append(" |\n");
        result.Append("{{#each ").Append(collection).Append(" }}| ")
            .AppendJoin(" | ", fields.Select(field => $"{{{{ {columns[field].Path} }}}}"))
            .Append(" |\n{{/each}}\n");
    }

    private static BiographyTemplateEditorException UnexpectedField(
        BiographyTemplateBlock block,
        BiographyTemplateField field) =>
        new($"Feltet `{field}` kan ikke bruges i blokken {BiographyTemplateEditorCatalog.GetBlock(block.Type).Label}.");

    private static readonly IReadOnlyDictionary<BiographyTemplateField, TableColumn> EventColumns =
        new Dictionary<BiographyTemplateField, TableColumn>
        {
            [BiographyTemplateField.EventCategory] = new("Kategori", "category"),
            [BiographyTemplateField.EventType] = new("Type", "type"),
            [BiographyTemplateField.EventValue] = new("Oplysning", "value"),
            [BiographyTemplateField.EventDate] = new("Dato", "date"),
            [BiographyTemplateField.EventPlace] = new("Sted", "place"),
        };

    private static readonly IReadOnlyDictionary<BiographyTemplateField, TableColumn> CensusColumns =
        new Dictionary<BiographyTemplateField, TableColumn>
        {
            [BiographyTemplateField.CensusDate] = new("Dato", "date"),
            [BiographyTemplateField.CensusPlace] = new("Sted", "place"),
            [BiographyTemplateField.CensusNote] = new("Note", "note"),
        };

    private static readonly IReadOnlyDictionary<BiographyTemplateField, TableColumn> SourceColumns =
        new Dictionary<BiographyTemplateField, TableColumn>
        {
            [BiographyTemplateField.SourceTitle] = new("Titel", "title"),
            [BiographyTemplateField.SourceAuthor] = new("Forfatter", "author"),
            [BiographyTemplateField.SourcePage] = new("Side", "page"),
        };

    private sealed record TableColumn(string Heading, string Path);
}
