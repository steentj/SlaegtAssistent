namespace SlaegtsAssistent.Core.Biography;

public static class BiographyTemplateEditorCatalog
{
    public static IReadOnlyList<BiographyTemplateBlockDescriptor> Blocks { get; } =
    [
        new(BiographyTemplateBlockType.Title, "Titel", "Personens navn som dokumentets titel."),
        new(BiographyTemplateBlockType.Facts, "Fakta", "Fødsel, død og nære relationer."),
        new(BiographyTemplateBlockType.Events, "Hændelser", "Personlige hændelser og familiehændelser."),
        new(BiographyTemplateBlockType.Census, "Folketællinger", "Registrerede folketællinger."),
        new(BiographyTemplateBlockType.Sources, "Kilder", "Kilder knyttet til personen."),
        new(BiographyTemplateBlockType.Media, "Medier", "Lokale billeder og andre medier."),
        new(BiographyTemplateBlockType.Submitter, "Afsender", "Afsenderoplysninger fra GEDCOM-filen."),
        new(BiographyTemplateBlockType.Biography, "Biografi", "Plads til brugerens egen livshistorie."),
    ];

    public static IReadOnlyList<BiographyTemplateFieldDescriptor> Fields { get; } =
    [
        new(BiographyTemplateField.PersonFullName, BiographyTemplateBlockType.Title, "Personens navn", ["person.fullName"]),
        new(BiographyTemplateField.Birth, BiographyTemplateBlockType.Facts, "Fødsel", ["person.birthDate", "person.birthPlace"]),
        new(BiographyTemplateField.Death, BiographyTemplateBlockType.Facts, "Død", ["person.deathDate", "person.deathPlace"]),
        new(BiographyTemplateField.Parents, BiographyTemplateBlockType.Facts, "Forældre", ["person.parents", "person.parentNames"]),
        new(BiographyTemplateField.Children, BiographyTemplateBlockType.Facts, "Børn", ["person.children", "person.childNames"]),
        new(BiographyTemplateField.Partners, BiographyTemplateBlockType.Facts, "Partnere", ["person.partners", "person.partnerNames"]),
        new(BiographyTemplateField.CreatedDate, BiographyTemplateBlockType.Facts, "Oprettelsesdato", ["person.createdDate"]),
        new(BiographyTemplateField.EventCategory, BiographyTemplateBlockType.Events, "Kategori", ["allEvents[].category"]),
        new(BiographyTemplateField.EventType, BiographyTemplateBlockType.Events, "Type", ["allEvents[].type"]),
        new(BiographyTemplateField.EventValue, BiographyTemplateBlockType.Events, "Oplysning", ["allEvents[].value"]),
        new(BiographyTemplateField.EventDate, BiographyTemplateBlockType.Events, "Dato", ["allEvents[].date"]),
        new(BiographyTemplateField.EventPlace, BiographyTemplateBlockType.Events, "Sted", ["allEvents[].place"]),
        new(BiographyTemplateField.CensusDate, BiographyTemplateBlockType.Census, "Dato", ["census[].date"]),
        new(BiographyTemplateField.CensusPlace, BiographyTemplateBlockType.Census, "Sted", ["census[].place"]),
        new(BiographyTemplateField.CensusNote, BiographyTemplateBlockType.Census, "Note", ["census[].note"]),
        new(BiographyTemplateField.SourceTitle, BiographyTemplateBlockType.Sources, "Titel", ["sources[].title"]),
        new(BiographyTemplateField.SourceAuthor, BiographyTemplateBlockType.Sources, "Forfatter", ["sources[].author"]),
        new(BiographyTemplateField.SourcePage, BiographyTemplateBlockType.Sources, "Side", ["sources[].page"]),
        new(BiographyTemplateField.MediaTitle, BiographyTemplateBlockType.Media, "Titel", ["media[].title"]),
        new(BiographyTemplateField.MediaRelativeFile, BiographyTemplateBlockType.Media, "Lokal fil", ["media[].relativeFile"]),
        new(BiographyTemplateField.SubmitterName, BiographyTemplateBlockType.Submitter, "Navn", ["submitter.name"]),
    ];

    public static IReadOnlyList<BiographyTemplatePresentationDescriptor> Presentations { get; } =
    [
        new(BiographyTemplatePresentation.Standard, "Standard"),
        new(BiographyTemplatePresentation.List, "Liste"),
        new(BiographyTemplatePresentation.Table, "Tabel"),
    ];

    public static BiographyTemplateBlockDescriptor GetBlock(BiographyTemplateBlockType type) =>
        Blocks.SingleOrDefault(item => item.Type == type)
        ?? throw new BiographyTemplateEditorException($"Ukendt bloktype `{type}`.");

    public static BiographyTemplateFieldDescriptor GetField(BiographyTemplateField field) =>
        Fields.SingleOrDefault(item => item.Field == field)
        ?? throw new BiographyTemplateEditorException($"Ukendt skabelonfelt `{field}`.");

    public static IReadOnlyList<BiographyTemplateFieldDescriptor> GetFields(BiographyTemplateBlockType type) =>
        Fields.Where(item => item.BlockType == type).ToArray();

    public static IReadOnlyList<BiographyTemplatePresentationDescriptor> GetPresentations(
        BiographyTemplateBlockType type)
    {
        BiographyTemplatePresentation[] allowed = type switch
        {
            BiographyTemplateBlockType.Events or
            BiographyTemplateBlockType.Census or
            BiographyTemplateBlockType.Sources =>
                [BiographyTemplatePresentation.List, BiographyTemplatePresentation.Table],
            BiographyTemplateBlockType.Facts => [BiographyTemplatePresentation.List],
            _ => [BiographyTemplatePresentation.Standard],
        };

        return Presentations.Where(item => allowed.Contains(item.Presentation)).ToArray();
    }
}

public sealed record BiographyTemplateBlockDescriptor(
    BiographyTemplateBlockType Type,
    string Label,
    string Description)
{
    public string Help => Type switch
    {
        BiographyTemplateBlockType.Title => "Viser personens navn øverst. Skjul sektionen, hvis navnet ikke skal gentages i dokumentet.",
        BiographyTemplateBlockType.Facts => "Vælg fødsel, død og relationer. Flyt felterne for at ændre rækkefølgen. Manglende oplysninger udelades.",
        BiographyTemplateBlockType.Events => "Vælg oplysninger om hændelser. Åbn Flere muligheder for at skifte mellem liste og tabel.",
        BiographyTemplateBlockType.Census => "Vis folketællingernes dato, sted og note. Åbn Flere muligheder for at vælge liste eller tabel.",
        BiographyTemplateBlockType.Sources => "Vælg kildens titel, forfatter og side. Flyt sektionen for at placere kilderne før eller efter hændelserne.",
        BiographyTemplateBlockType.Media => "Vis lokale billeder og medier. Fjern markeringen i Vis sektionen i dokumentet for at skjule dem.",
        BiographyTemplateBlockType.Submitter => "Vis navnet på GEDCOM-filens afsender. Sektionen udelades, når der ikke er afsenderoplysninger.",
        BiographyTemplateBlockType.Biography => "Tilføjer en overskrift og plads til livshistorien. Din egen tekst i eksisterende dokumenter bevares ved skabelonændringer.",
        _ => throw new BiographyTemplateEditorException($"Ukendt bloktype `{Type}`."),
    };

    public string Example => Type switch
    {
        BiographyTemplateBlockType.Title => "Anna Jensen",
        BiographyTemplateBlockType.Facts => "Født: 12. marts 1880 i Odense. Forældre: Jens Jensen og Marie Hansen.",
        BiographyTemplateBlockType.Events => "Vielse – 4. maj 1902 i Odense.",
        BiographyTemplateBlockType.Census => "1911 – Odense – Bopæl i Vestergade.",
        BiographyTemplateBlockType.Sources => "Kirkebog for Odense, side 42.",
        BiographyTemplateBlockType.Media => "Et lokalt familiefoto med billedteksten Familiefoto.",
        BiographyTemplateBlockType.Submitter => "Afsender: Karen Jensen.",
        BiographyTemplateBlockType.Biography => "Biografi – Skriv biografien her.",
        _ => throw new BiographyTemplateEditorException($"Ukendt bloktype `{Type}`."),
    };
}

public sealed record BiographyTemplateFieldDescriptor(
    BiographyTemplateField Field,
    BiographyTemplateBlockType BlockType,
    string Label,
    IReadOnlyList<string> TechnicalPaths);

public sealed record BiographyTemplatePresentationDescriptor(
    BiographyTemplatePresentation Presentation,
    string Label);
