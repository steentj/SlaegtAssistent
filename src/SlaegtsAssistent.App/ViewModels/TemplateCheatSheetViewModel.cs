using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public partial class TemplateCheatSheetViewModel : ViewModelBase
{
    private static readonly SafeMarkdownPreviewService PreviewService = new();

    public static readonly string Content =
        "# Hjælp til persondokumentskabeloner\n\n" +
        "## Åbn og tilpas\n\n" +
        "Åbn Indstillinger og vælg Rediger skabelon…. Du behøver ikke kende et skabelonsprog. " +
        "Vælg en sektion i listen til venstre. Brug Flyt op og Flyt ned til at ændre rækkefølgen. " +
        "Fjern markeringen i Vis sektionen i dokumentet for at skjule den. " +
        "Overskrift ændrer kun den valgte sektions overskrift. Markér de ønskede felter under Indhold.\n\n" +
        "## Kontrollér resultatet\n\n" +
        "Vælg fanen Forhåndsvisning. Et lokalt, opdigtet eksempel virker også uden GEDCOM. " +
        "Hvis personer er indlæst, kan du vælge en af dem under Vis med. " +
        "Resultatet opdateres automatisk. Ved fejl bevares den sidste gyldige forhåndsvisning.\n\n" +
        "## Gem eller annuller\n\n" +
        "Brug kladde overfører ændringerne til Indstillinger, men skriver endnu ikke en fil. " +
        "Vælg Gem eller Gem som… i Indstillinger for at gemme skabelonen. " +
        "Annuller i editoren kasserer kladden; Annuller i Indstillinger lader fil og aktiv indstilling være uændret. " +
        "Fortryd og Gendan går tilbage og frem i ændringerne. Nulstil genskaber standarden efter bekræftelse.\n\n" +
        "## Opdatér eksisterende dokumenter\n\n" +
        "Vælg Anvend skabelon i hovedvinduet for at gennemgå ændringer uden at genindlæse GEDCOM. " +
        "Godkend kun de ønskede ændringer. Fri tekst og godkendte personoplysninger bevares. " +
        "Åbne dokumenter med ugemte ændringer forbliver ugemte.\n\n" +
        "## Tastatur\n\n" +
        "Tab og Skift+Tab flytter fokus. Piletaster vælger sektioner og felter. Mellemrum slår et valg til eller fra. " +
        "Alt+Pil op eller ned flytter den valgte sektion. Ctrl+Alt+Pil op eller ned flytter det valgte felt. " +
        "Ctrl+Z fortryder, Ctrl+Y gendanner. Escape annullerer editoren.\n\n" +
        "## Tidligere skabeloner\n\n" +
        "Gamle håndskrevne skabeloner konverteres ikke. Opret en ny skabelon ud fra standarden " +
        "og brug Gem som… for at bevare den gamle fil.\n\n" +
        string.Join("\n\n", BiographyTemplateEditorCatalog.Blocks.Select(block =>
            $"## {block.Label}\n\n{block.Help}\n\nEksempel: {block.Example}"));

    [ObservableProperty]
    private string searchText = string.Empty;

    public string FilteredContent => string.IsNullOrWhiteSpace(SearchText)
        ? Content
        : FilterSections(SearchText.Trim());

    private static string FilterSections(string query)
    {
        var matches = Content.Split("\n\n## ")
            .Select((section, index) => index == 0 ? section : "## " + section)
            .Where(section => section.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();
        return matches.Length == 0 ? "Ingen match fundet." : string.Join("\n\n", matches);
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
