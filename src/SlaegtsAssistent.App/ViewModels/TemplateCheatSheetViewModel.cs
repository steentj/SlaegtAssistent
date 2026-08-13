using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Markdig;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.ViewModels;

public partial class TemplateCheatSheetViewModel : ViewModelBase
{
    private static readonly MarkdownPipeline MarkdownPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public static readonly string Content =
        "# Skabelon-cheat sheet\n\n" +
        $"Offentlig feltkontrakt version {BiographyTemplateContract.CurrentVersion}.\n\n" +
        "Skabeloner er almindelig Markdown med sikre felter, betingelser og løkker. " +
        "Skabeloner kan ikke kalde C#-metoder, læse filer eller starte processer.\n\n" +
        "## Felter\n\n" +
        "Skriv et felt med dobbelte krøllede parenteser:\n\n" +
        "`{{ person.fullName }}`\n\n" +
        "### Person\n\n" +
        "- `person.recordId`\n" +
        "- `person.fullName`\n" +
        "- `person.sex`\n" +
        "- `person.birthDate`, `person.birthPlace`\n" +
        "- `person.deathDate`, `person.deathPlace`\n" +
        "- `person.parents`\n\n" +
        "### Afsender (`SUBM`)\n\n" +
        "- `submitter.recordId`\n" +
        "- `submitter.name`, `submitter.address`\n" +
        "- `submitter.phone`, `submitter.email`\n" +
        "- `submitter.website`, `submitter.language`\n\n" +
        "## Betingelser\n\n" +
        "En betingelse viser kun indhold, når feltet har en værdi:\n\n" +
        "{{#if person.birthDate}}\n" +
        "**Født:** {{ person.birthDate }}\n" +
        "{{/if}}\n\n" +
        "Der findes ikke en `else`-gren. Brug flere `if`-blokke i stedet.\n\n" +
        "## Løkker\n\n" +
        "Brug `each` til lister. Inde i løkken bruges det aktuelle elements felter:\n\n" +
        "{{#each person.parents}}\n" +
        "- {{ fullName }} ({{ recordId }})\n" +
        "{{/each}}\n\n" +
        "## Hændelser\n\n" +
        "Tilgængelige lister:\n\n" +
        "- `events` – personens hændelser\n" +
        "- `familyEvents` – hændelser fra personens familier\n" +
        "- `allEvents` – begge grupper samlet\n" +
        "- `census` – folketællinger\n\n" +
        "Hændelsesfelter: `tag`, `category`, `value`, `date`, `place`, `type`, `note` og `sources`.\n\n" +
        "{{#each allEvents}}\n" +
        "- **{{ category }}**{{#if type}} ({{ type }}){{/if}}{{#if date}} – {{ date }}{{/if}}{{#if place}} i {{ place }}{{/if}}\n" +
        "{{/each}}\n\n" +
        "## Kilder og medier\n\n" +
        "Listerne `sources` og `media` kan bruges direkte:\n\n" +
        "{{#each sources}}\n" +
        "- {{ title }}{{#if author}} ({{ author }}){{/if}}{{#if page}}, side {{ page }}{{/if}}\n" +
        "{{/each}}\n\n" +
        "{{#each media}}\n" +
        "![{{ title }}]({{ relativeFile }})\n" +
        "{{/each}}\n\n" +
        "Mediefelter er `recordId`, `file`, `relativeFile`, `form`, `title`, `type` og `note`.\n\n" +
        "## Tabel-eksempel\n\n" +
        "Almindelig Markdown kan bruges til tabeller:\n\n" +
        "| Felt | Skabelon |\n" +
        "| --- | --- |\n" +
        "| Navn | `{{ person.fullName }}` |\n" +
        "| Fødselsdato | `{{ person.birthDate }}` |\n\n" +
        "En dynamisk hændelsestabel kan skrives sådan:\n\n" +
        "```text\n" +
        "| Dato | Sted | Hændelse |\n" +
        "| --- | --- | --- |\n" +
        "{{#each allEvents}}\n" +
        "| {{ date }} | {{ place }} | {{ category }} |\n" +
        "{{/each}}\n" +
        "```\n\n" +
        "## Standardskabelon\n\n" +
        "Den indbyggede standardskabelon viser navn, fakta, hændelser, census, kilder, medier " +
        "og afsender. Den kan bruges som udgangspunkt for en egen global skabelon.\n\n" +
        "## Normativ feltliste\n\n" +
        ContractFieldList + "\n";

    public static string ContractFieldList => string.Join(
        "\n",
        BiographyTemplateContract.PublicFieldPaths.Select(path => $"- `{path}`"));

    [ObservableProperty]
    private string searchText = string.Empty;

    public string FilteredContent
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return Content;
            }

            var matchingLines = Content
                .Split('\n')
                .Where(line => line.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase))
                .ToArray();

            return matchingLines.Length == 0
                ? "Ingen match fundet."
                : string.Join(Environment.NewLine, matchingLines);
        }
    }

    public string PreviewHtml => Markdig.Markdown.ToHtml(FilteredContent, MarkdownPipeline);

    public string PreviewHtmlDocument =>
        $"<!doctype html><html><head><meta charset=\"utf-8\"><style>" +
        "body{font-family:system-ui,sans-serif;line-height:1.55;margin:24px;color:#23313a;background:#FFFFFF;}" +
        "h1,h2,h3{line-height:1.2;color:#174a5b;}" +
        "code,pre{background:#edf2f3;padding:2px 4px;}" +
        "table{border-collapse:collapse;margin:12px 0;}th,td{border:1px solid #9aaeb5;padding:6px 10px;text-align:left;}" +
        "</style></head><body>" +
        PreviewHtml +
        "</body></html>";

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredContent));
        OnPropertyChanged(nameof(PreviewHtml));
        OnPropertyChanged(nameof(PreviewHtmlDocument));
    }
}
