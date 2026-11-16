# Plan: Billeder ved markøren i livsforløbet

Status: **planlagt**. Estimat: **2–3 udviklingsdage**. Afhænger af feature: 04.

Tilføj en Indsæt billede-handling, som indsætter én billedparagraf i fri biografitekst og kan gentages efter behov.

Kontrakter: [Mediekontrakt.md](../Mediekontrakt.md). Krav: [Requirements.md](Requirements.md). Validering: [Validation.md](Validation.md).

## Implementeringstrin

1. Opret `EditorSelection` og bufferrevision uden Avalonia-afhængigheder. Udbyg tekstfeltadapteren fra feature 04 i `MainWindow.axaml(.cs)` med caret/markering og fokusgendannelse; brug samme vej for topbilledeændringer.
2. Udvid `BiographyImageEditService` med kontrol af fri tekst, markering og billedindsættelse efter kontrakten. Bevar dansk tekst, emoji og eksisterende linjeafslutninger uden indeksfejl.
3. Tilføj Indsæt billede… ved editoren. Uden editor eller under arbejdsområdespærring er kommandoen deaktiveret. Markør i genereret sektion giver den aftalte besked og ingen implicit flytning.
4. Indfang markering og revision før vælger åbnes; anvend kun resultat i uændret kontekst. Indsæt nødvendige tomme linjer, Markdown-billede og valgfri billedtekst; opdatér markør til efter det nye afsnit.
5. Erstat kun markering, som er helt inden for fri tekst. Topblok, genererede fakta eller en markering hen over grænsen afvises. Ældre dokumenter uden generated-blok følger kontraktens fallback.
6. Afprøv gentagen indsættelse, fortryd/gentag, autoskift til preview, almindelig Gem, lukkeadvarsel, ugemt eksport og Commit. Opdater Markdown-hjælpen med betjeningsvej og resulterende syntaks.

## Afslutningskriterium

Alle featurekrav er dækket af relevante tests, det manuelle flow er gennemført, og resultaterne er skrevet i Validation.md. Fejlende tests skrives før adfærdsændringer i stiopslag, tekstoperationer og kandidatbevaring. Featureen må ikke erklæres færdig alene på baggrund af UI-skærmbilleder.
