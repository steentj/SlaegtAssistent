# Plan: Udvalg, batch og udskrift

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

7C feature 01 med rigtig DOCX/PDF/ODT-prøve.

Kravsporbarhed: **B-06, B-07, B-08**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Byg `BiographyExportViewModel` med udvalg af record-id'er fra hele gyldige katalog, søgning, format og samlet/separat tilstand. Aktuel person er forvalgt; vis også ikke-eksportérbare dokumenter med konkret årsag, så de ikke forsvinder i stilhed.
2. Implementér preflight for 1–100 personer, dokumentfejl, filnavne, aktiver og alle mål. Frys tekster/udvalg ved start; vis tydeligt at ugemte ændringer medtages uden autosave.
3. Genbrug fil-/mappepicker og overskrivningsbekræftelse. Implementér stabile filnavne, case-insensitive sammenfald og reserverede Windows-navne i en ren testbar navnefunktion.
4. Implementér `BiographyExportService` med sekventiel konvertering og validering af alle outputs før publicering. Faserne vises som forbereder/konverterer/gemmer med antal; dobbeltstart afvises.
5. Publicér atomisk pr. fil og saml præcist resultat, også ved diskfejl/annullering midt i publicering. Håndtér joboprydning og åbne dokumenter, som redigeres efter snapshots uden at ændre igangværende eksport.
6. Tilføj Udskriv samlet biografi-PDF med 7B's launcher og instruktion. Udskriftsjob bruger samme snapshot og preflight som eksport.
7. Skriv `BiographyExportViewModelTests`, `BiographyExportBatchTests` og headless dialogtests; verificér native labels, fokus, valg og fejlliste.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
