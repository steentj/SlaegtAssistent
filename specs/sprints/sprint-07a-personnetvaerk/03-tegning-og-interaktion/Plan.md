# Plan: Tegning og interaktion

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

Feature 01–02. Brug syntetisk graf i en lokal testvært før arbejdsbordintegration.

Kravsporbarhed: **G-05, G-06, G-07, G-08, G-12**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Opret `PersonGraphControl` og `PersonGraphView.axaml` med én tegneflade, overlay og native nabo-/søgeliste. Opret rene formattere og ViewModel-data til navn, dato, kønskategori og fremhævelse.
2. Implementér kamera, vis alle, cursorforankret zoom og centrér proband. Udtræk ren kameramatematik til testbar klasse med double-koordinater, ikke Avalonia-typer i ViewModel.
3. Tegn alle synlige kanter før prikkerne. Brug temaressourcer, kønsfarver og probandens størrelse/ring. Implementér spatialt hit-testindeks med mindst 8 DIP hit-radius; ved overlap vælges nærmeste centrum, derefter ordinalt id.
4. Implementér hover/fokus som en node plus alle dens urettede naboer. Tegn labels med fulde eksisterende datoer og separat overlapplacering; gentænk ikke layoutet. Hold nabolisten tilgængelig som kontrakten angiver.
5. Skeln klik fra træk ved 4 DIP. Emitér kun record-id til ViewModelens åbningskommando; filåbning og probandskift hører ikke til kontrollen. Pointercapture frigives også ved exit/annulleret input.
6. Tilføj keyboardbetjening i native lister og værktøjslinje med automation-navne. Piletaster vælger listeperson, Enter åbner og Escape rydder oplysninger; tastaturvalg anvender samme nabodata som hover.
7. Skriv `GraphCameraTests`, `PersonGraphViewModelTests` og headless kontroltests. Mål redraw/hit-test med 10.000 noder uden én Avalonia-kontrol pr. node; afmeld events når viewet lukkes.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
