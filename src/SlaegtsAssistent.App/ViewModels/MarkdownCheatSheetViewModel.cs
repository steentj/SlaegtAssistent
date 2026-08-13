using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MarkdownCheatSheetViewModel : ViewModelBase
{
    private static readonly SafeMarkdownPreviewService PreviewService = new();

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

    public string PreviewHtml => PreviewService.RenderHelp(FilteredContent).Html;

    public string PreviewHtmlDocument => PreviewService.RenderHelp(FilteredContent).HtmlDocument;

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredContent));
        OnPropertyChanged(nameof(PreviewHtml));
        OnPropertyChanged(nameof(PreviewHtmlDocument));
    }
}
