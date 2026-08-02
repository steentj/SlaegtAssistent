using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Markdig;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MarkdownCheatSheetViewModel : ViewModelBase
{
    private static readonly MarkdownPipeline MarkdownPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public const string Content =
        "# Markdown-cheat sheet\n\n" +
        "## Overskrifter\n\n" +
        "# Niveau 1\n" +
        "## Niveau 2\n" +
        "### Niveau 3\n\n" +
        "## Afsnit\n\n" +
        "Skriv almindelig tekst som et afsnit. " +
        "Start et nyt afsnit med en tom linje.\n\n" +
        "## Fremhævet tekst\n\n" +
        "**Fed tekst**\n\n" +
        "*Kursiv tekst*\n\n" +
        "~~Gennemstreget tekst~~\n\n" +
        "## Lister\n\n" +
        "- Første punkt\n" +
        "- Andet punkt\n\n" +
        "1. Første trin\n" +
        "2. Andet trin\n\n" +
        "## Links\n\n" +
        "[Slægtsassistent](https://example.dk)\n\n" +
        "## Billeder\n\n" +
        "![Beskrivelse](medier/billede.jpg)\n\n" +
        "Brug relative stier til billeder i persondokumenterne.\n\n" +
        "## Tabeller\n\n" +
        "| Navn | År |\n" +
        "| --- | ---: |\n" +
        "| Anna Jensen | 1900 |\n" +
        "| Jens Hansen | 1898 |\n\n" +
        "## Linjeskift og citater\n\n" +
        "Brug to mellemrum i slutningen af en linje  \n" +
        "for et manuelt linjeskift.\n\n" +
        "> Dette er et citat.\n";

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

    public string PreviewHtml => Markdown.ToHtml(FilteredContent, MarkdownPipeline);

    public string PreviewHtmlDocument =>
        $"<!doctype html><html><head><meta charset=\"utf-8\"><style>" +
        "body{font-family:system-ui,sans-serif;line-height:1.55;margin:24px;color:#23313a;}" +
        "h1,h2,h3{line-height:1.2;color:#174a5b;}" +
        "table{border-collapse:collapse;margin:12px 0;}th,td{border:1px solid #9aaeb5;padding:6px 10px;text-align:left;}" +
        "blockquote{border-left:4px solid #6e9eaa;margin-left:0;padding-left:12px;color:#4d626a;}" +
        "code{background:#edf2f3;padding:2px 4px;}" +
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
