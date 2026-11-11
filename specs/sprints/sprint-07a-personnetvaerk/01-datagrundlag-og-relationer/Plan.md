# Plan: Datagrundlag og relationer

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

Ingen tidligere feature i sprintet. Læs ../Grafkontrakt.md og de eksisterende dokument-/baseline-modeller.

Kravsporbarhed: **G-01, G-02, G-03**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Opret `Core/Graph/` med kontraktens input-, node-, relations- og graftyper. Lad builderen tage færdigt normaliserede inputlister og returnere et uforanderligt resultat med diagnostik og naboindeks.
2. Skriv `PersonGraphBuilderTests` for den faste 10-personers-fixture, gentagne familier, isolerede personer, løkker og flere årsager på samme kant. Assertér præcise id-mængder, ikke kun antal.
3. Implementér ordinalt normaliserede nodepar, deduplikering af årsager og urettet naboopslag. Gem relationens retning uden at tegne pile.
4. Opret `WorkspaceGraphSource` i App. Tag snapshot fra hele kataloget og editorcache; læs dokumenter uden åbne editorer. Implementér facts/Approved/reserve-prioritet og diagnostik ved tvetydighed.
5. Udvid `IGedcomSnapshotStore` med en læseoperation for hashvalideret lokal kildekopi, fx `GetValidatedSourceCopyPath(outputDirectory)`. Hold manifestdetaljer inde i store, kontrollér fysisk sti, og opdatér eksisterende testfakes. Brug `IGedcomLoader` uden at udføre import-/gemmekommandoer.
6. Udtræk Markdig-links i App og genbrug sikker linkopløsning. Cachen nøgler på dokumentrevision og katalogrevision; åbne buffers opdateres med debounce. Kopiér data før baggrundsarbejde, så mutérbare domæneobjekter ikke læses samtidig med import.
7. Tilføj `WorkspaceGraphSourceTests` for genstart uden originalfil, dokumenter uden baseline, afviste forældre og ændrede bufferlinks. Tilslut endnu ikke nyt UI; demonstrér snapshot gennem tests/testvært.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
