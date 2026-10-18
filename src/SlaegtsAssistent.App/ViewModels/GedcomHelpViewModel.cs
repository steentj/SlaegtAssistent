using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class GedcomHelpViewModel : ViewModelBase
{
    private static readonly SafeMarkdownPreviewService PreviewService = new();

    public const string Content = """
        # GEDCOM – slægtsforskningens digitale fællessprog

        **GEDCOM** er en forkortelse for **GE**nealogical **D**ata **COM**munication. Det er et åbent filformat og den **de facto-standard**, der bruges til at gemme og udveksle slægtsdata mellem slægtsforskningsprogrammer og hjemmesider.

        - **Oprindelse:** Formatet blev udviklet i 1984 af Jesu Kristi Kirke af Sidste Dages Hellige (LDS-kirken) for at gøre det lettere at dele forskning i et ensartet, computerlæsbart format.
        - **Anvendelse:** Næsten alle slægtsplatforme (fx Ancestry, MyHeritage og FamilySearch) kan importere og eksportere GEDCOM. Det er derfor den almindelige måde at tage backup og flytte et slægtstræ fra ét program til et andet.
        - **Filtype:** GEDCOM-filer har typisk endelsen `.ged`.

        ## De basale elementer

        En GEDCOM-fil er en **almindelig tekstfil**, opbygget hierarkisk. Hver linje består af et niveautal, et tag og ofte en værdi eller en reference.

        1. **Niveau:** Starter ved `0` for hovedposter (personer, familier, kilder) og stiger (`1`, `2`, `3`…) for underliggende detaljer.
        2. **Tag:** Korte koder på 3-4 bogstaver, fx `INDI` (individ) og `BIRT` (fødsel).
        3. **Krydsreference-id:** Unikke id'er som `@I1@`, der bruges til at linke poster sammen og undgå dubletter.

        ### Eksempel 1: En person

        ```
        0 @I1@ INDI            (niveau 0: ny personpost med id @I1@)
        1 NAME Hans /Jensen/   (navn; efternavnet står mellem skråstreger)
        1 SEX M                (køn: mand)
        1 BIRT                 (begivenhed: fødsel)
        2 DATE 12 MAY 1920     (under fødsel: dato)
        2 PLAC Aarhus, DK      (under fødsel: sted)
        ```

        ### Eksempel 2: En familie

        ```
        0 @F1@ FAM             (niveau 0: ny familiepost med id @F1@)
        1 HUSB @I1@            (ægtemand er person @I1@)
        1 WIFE @I2@            (hustru er person @I2@)
        1 CHIL @I3@            (barn er person @I3@)
        1 MARR                 (begivenhed: vielse)
        2 DATE 1 JUN 1945      (vielsesdato)
        ```

        ### Eksempel 3: Tre generationer

        Personer og familier kobles med `FAMS` (familien, hvor personen er forælder) og `FAMC` (familien, hvor personen er barn).

        ```
        0 @I1@ INDI            (bedstefar)
        1 NAME Ole /Jensen/
        1 FAMS @F1@

        0 @I2@ INDI            (far)
        1 NAME Hans /Jensen/
        1 FAMC @F1@            (barn i familie F1)
        1 FAMS @F2@            (forælder i familie F2)

        0 @I3@ INDI            (barnebarn)
        1 NAME Erik /Jensen/
        1 FAMC @F2@            (barn i familie F2)

        0 @F1@ FAM             (bedsteforældrenes familie)
        1 HUSB @I1@
        1 CHIL @I2@

        0 @F2@ FAM             (forældrenes familie)
        1 HUSB @I2@
        1 CHIL @I3@
        ```

        ## Almindelige posttyper

        | Type | Tag | Indhold (eksempler) | Eksempel |
        |------|-----|---------------------|----------|
        | Individ | `INDI` | Navn, køn, fødsel, død, titler | `0 @I1@ INDI` |
        | Familie | `FAM` | Reference til mand, kone og børn, vielse | `0 @F1@ FAM` |
        | Kilde | `SOUR` | Titel, forfatter, udgivelsesdata | `0 @S1@ SOUR` |
        | Multimedie | `OBJE` | Filsti til billede, format, titel | `0 @O1@ OBJE` |
        | Header | `HEAD` | Programnavn, version, tegnsæt (UTF-8) | `0 HEAD` |
        | Note | `NOTE` | Tekstlige kommentarer fra forskeren | `0 @N1@ NOTE` |

        ## Sådan behandler SlægtsAssistenten tags

        - **Hændelser** (fx `BIRT`, `DEAT`, `MARR`, `OCCU`) vises som hændelser i biografien.
        - **`CREA`** (oprettelsesdato) er metadata om, hvornår en post blev oprettet, og er ikke en hændelse. Datoen er tilgængelig i skabeloner som `person.createdDate` ("Oprettet dato") og kan udelades i skabelonen.
        - **Private tags** (starter med `_`, fx `_MYTAG`) er programspecifikke og medtages ikke som hændelser.
        - **`CHAN`** (seneste ændring) er metadata og vises ikke som hændelse.

        ## Almindelige problemer og faldgruber

        - **Private tags:** Mange programmer bruger egne tags, der starter med en understregning (`_TAG`). Andre programmer forstår dem ofte ikke og ignorerer dem, hvilket kan give datatab ved flytning.
        - **Tegnsæt:** Ældre filer bruger ANSEL eller ANSI. Læses de som UTF-8 (eller omvendt), kan bogstaver som **æ, ø og å** blive ødelagt.
        - **Multimediefiler:** En GEDCOM-fil indeholder kun tekst. Billeder og dokumenter er links til placeringer på en computer. Flyttes filen uden billederne, brydes forbindelsen.
        - **Datatab ved import:** Nogle programmer læser ikke alle niveauer (fx noter på begivenheder), så information kan gå tabt.
        - **Kronologi:** Mangler fødselsdatoer, kan rækkefølgen af børn i en familie blive forkert, fordi standarden ikke altid tvinger en bestemt sortering.
        - **Versioner:** Der findes flere versioner (5.5, 5.5.1 og 7.0), og de er ikke altid fuldt kompatible. 5.5.1 er mest udbredt; 7.0 er nyere med bedre understøttelse af multimedier, men ikke fuldt implementeret overalt.

        ## Reference

        Den officielle specifikation findes på GEDCOM-sitet (alle adresser er kun til orientering; SlægtsAssistenten henter intet fra internettet):

        - GEDCOM-specifikationer: `https://gedcom.io/specifications/`
        - GEDCOM 5.5.1: `https://gedcom.io/specifications/ged551.pdf`
        - GEDCOM 7.0: `https://gedcom.io/specifications/FamilySearchGEDCOMv7.html`
        """;

    public string PreviewHtml => PreviewService.RenderHelp(Content).Html;

    public string PreviewHtmlDocument => PreviewService.RenderHelp(Content).HtmlDocument;
}
