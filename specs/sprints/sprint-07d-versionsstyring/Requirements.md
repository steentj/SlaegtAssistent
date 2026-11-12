# Requirements: sprint 7D – versionsstyring

ID'erne bruges i tests og i Validation.md. Beslutningsgrundlaget står i [Analyse.md](Analyse.md).

## Versionsstyring og omfang

- **VS-1** Versionsstyring bruger Git lokalt i arbejdsmappen (`StandardMarkdownOutputFolder`). Ingen netværkskald, ingen remote, ingen telemetri.
- **VS-2** Kun Markdown-dokumenter direkte i arbejdsmappen (`*.md`) versioneres. `.slaegtsassistent/` og andre filer versioneres ikke og tælles ikke som ændringer.
- **VS-3** Appen ændrer aldrig brugerens globale Git-konfiguration, kører aldrig kommandoer mod et overliggende arkiv, og springer hooks over.
- **VS-4** Mangler Git, er versionsfunktionerne deaktiverede med en dansk forklaring; resten af appen er upåvirket.
- **VS-5** Er mappen ikke versioneret, tilbydes "Slå versionsstyring til" (initialiserer arkiv, `.gitignore`, arkivlokal identitet og en start-commit) efter bekræftelse.
- **VS-6** Ligger arbejdsmappen inde i et andet Git-arkiv, deaktiveres funktionen med forklaring; det overliggende arkiv ændres ikke.
- **VS-7** Tegn som æøå og mellemrum i filnavne og commit-beskeder bevares korrekt på alle platforme. Linjeskift i dokumenter bevares byte-for-byte.
- **VS-8** Ingen ny NuGet-pakke og ingen ændring af lockfiler.

## Commit

- **CM-1** Hovedvinduet har en separat knap **Commit**, der gemmer alle åbne editorer og derefter stager og committer alle ændringer (ændrede, nye, slettede dokumenter) i én commit.
- **CM-2** Brugeren angiver commit-besked i en lille dialog med forudfyldt forslag `Ændringer dd-MM-yyyy HH:mm`. Annullering eller blank besked opretter ingen commit.
- **CM-3** Mislykkes gemning af editorer, committes der ikke.
- **CM-4** Uden ændringer vises en besked, og der oprettes ingen tom commit.
- **CM-5** Commit er deaktiveret under GEDCOM-import.
- **CM-6** Fejl vises på dansk uden stakspor og med en mulig handling, hvor det er relevant.

## Versionshistorik-vindue

- **VH-1** Et separat, ikke-modalt vindue "Versionshistorik" åbnes fra knappen **Versionshistorik** i hovedvinduet. Højst ét vindue; gentaget åbning fokuserer det eksisterende.
- **VH-2** Listen viser pr. version dato (`dd-MM-yyyy`), tid (`HH:mm`, lokal tid) og commit-besked, kronologisk med nyeste øverst. Maks. 500 versioner; en note vises, hvis der er flere.
- **VH-3** Hver version har knapperne **Vis ændringer** og **Rul tilbage her**.
- **VH-4** Øverst vises en statuslinje med: om der er ikke committede ændringer, antallet (dokumenter), og dato/tid for seneste ændring (filsystemets ændringstidspunkt; "tidspunkt ukendt" hvis alle ændringer er sletninger). Uden ændringer vises "Ingen ikke committede ændringer."
- **VH-5** Statuslinjen har knapperne **Vis ændringer** og **Commit alle**; begge er deaktiveret uden ændringer og under en igangværende handling eller import.
- **VH-6** Statuslinjen oplyser, hvis der er ugemte editorændringer, og at de gemmes ved commit.
- **VH-7** Vinduet opdateres ved åbning, efter hver handling, når dokumenter gemmes i hovedvinduet, og når vinduet aktiveres.
- **VH-8** Vinduet kan betjenes med tastatur og har tilgængelige navne på knapper og liste; tekster er på dansk; farver følger lyst/mørkt tema.

## Vis ændringer

- **VÆ-1** Vis ændringer (for en version eller for ikke committede ændringer) åbner et separat, ikke-modalt vindue med liste over berørte filer og en linjeopdelt, farvet visning af tilføjede, fjernede og uændrede linjer.
- **VÆ-2** Ikke committede, nye filer vises som tilføjede; visningen ændrer ikke antallet af ikke committede ændringer.
- **VÆ-3** En versions ændringer er ændringerne i netop den version (rodversionen viser alle filer som nye).
- **VÆ-4** Meget store ændringer afkortes (maks. 20 000 linjer) med en tydelig notits; for store til at hente (> 8 mio. tegn) giver en venlig fejl.
- **VÆ-5** Visningen er skrivebeskyttet.

## Rul tilbage

- **RT-1** Rul tilbage her kræver en bekræftelse, der nævner versionens dato, tid og besked og forklarer, at nuværende tilstand gemmes som en ny version først.
- **RT-2** Tilbagerulningen er **ikke-destruktiv**: ugemte editorer gemmes, ikke committede ændringer committes automatisk som "Automatisk version før tilbagerulning", dokumenterne gendannes til den valgte version (gendannede, fjernede og ændrede filer), og resultatet committes som `Rul tilbage til <dato tid>: <besked>`. Ingen historik slettes eller omskrives; `reset --hard` bruges aldrig.
- **RT-3** Er dokumenterne allerede i den valgte tilstand, oprettes ingen ny version, og brugeren får besked.
- **RT-4** Efter tilbagerulning genindlæses arbejdsområdet (personliste, editorer, preview, graf) fra de gendannede filer; ingen ugemt tekst går tabt.
- **RT-5** Mislykkes gendannelsen, føres dokumenterne tilbage til den seneste version, og brugeren får en tydelig fejl.
- **RT-6** Commit-id'er valideres (`^[0-9a-f]{40,64}$`) og bekræftes som commit, før de bruges i en kommando.
- **RT-7** Tilbagerulning er deaktiveret under import og for den nyeste version, når der ikke er ikke committede ændringer.

## Robusthed og sikkerhed

- **RS-1** Git køres aldrig via shell; argumenter sendes som liste. Sti- og id-argumenter kan ikke tolkes som flag (sti efter `--`, id valideret).
- **RS-2** Versionskommandoer kører én ad gangen, har tidsgrænse (30–60 sek.) og kan annulleres; hovedtråden blokeres ikke.
- **RS-3** `index.lock`-konflikter og andre Git-fejl oversættes til danske beskeder.
- **RS-4** `LocalProcessRunner`-udvidelsen er bagudkompatibel; Graphviz-kald opfører sig uændret.
- **RS-5** Core-delen har ingen I/O og ingen Avalonia-afhængighed; ViewModels bruger ingen Avalonia-typer.
- **RS-6** Tests mod rigtig Git rører ikke brugerens globale konfiguration og rydder deres midlertidige mapper; manglende Git registreres som *ikke kørt*.
