# Plan: Grafeksport og udskriftsflow

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

7B feature 01 og 7A færdig grafvisning.

Kravsporbarhed: **X-03, X-04, X-05, X-06, X-07 samt samlet X-01–X-08**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Implementér ren `GraphExportSceneBuilder` med hele grafen/udsnit, signatur, lyse farver, proband og valgfri tekst. Brug samme formatering som hover, men eksporter aldrig transient hover/fokus.
2. Skriv `GraphExportSceneTests` for præcis node-/kantmængde, clipping af krydsende kanter, labelbounds og stable output. Implementér `GraphSvgWriter` med XML-escaping og invariant talformat.
3. Implementér `AvaloniaGraphRasterizer` og PDF-adapteren til sidecaren. Kontrollen må ikke selv panorere, skjule labels eller ændre tema under eksport.
4. Byg `GraphExportViewModel` og dialog med format, område, labels, PNG-størrelse eller PDF-papir/retning. Fastfrys grafrevision og kamera ved start, deaktivér dobbeltstart og tilbyd annullering.
5. Tilføj save-picker, kontrolleret overskrivningsbekræftelse og atomisk publicering. Afvis urimelige dimensioner/output før stor allocation, og bevar den gamle fil ved fejl.
6. Implementér `ISystemDocumentLauncher` og Udskriv-handlingen med genereret PDF og ærlig dansk status. Åbn kun valideret outputfil efter brugerens eksplicitte klik; ved fejl tilbydes Gem PDF.
7. Tilføj App-tests for dialog, fejl, annullering, launcher og opdateret graf under eksport. Gennemfør visuel eksport-/printvalidering og hele 7B-journalen.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
