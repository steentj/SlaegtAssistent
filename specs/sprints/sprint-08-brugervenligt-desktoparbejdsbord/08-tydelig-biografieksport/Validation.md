# Validering: Gør eksportens tilstand og handlinger tydelige

Status: **delvist udført**. Automatiserede prøver er kørt; manuel og native afprøvninger udestår.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Prøv tomt udvalg, manglende værktøj, langt statusindhold og annullering i flere faser.
2. Kontrollér minimum 540 × 500 og at overskrivningskontrol, advarsler og PDF-åbning fungerer.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Ændret lukning kan frigive ressourcer før jobafslutning. Genbrug cancellation og snapshots; ændr ikke eksportkontrakter. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 2026-11-17, macOS | Restore, build og automatiserede prøver | Restore og build bestået; 645 prøver bestået, 0 fejlet, 6 oversprunget. | De oversprungne prøver omfatter eksportværktøj og eksport af ugemte redigeringer. |
| 2026-11-17 | R8-01, tilstandsovergange | Regression dækker tomt udvalg, valg og igangværende eksport. | Manuel kontrol udestår. |
| Ikke udført | Manuel kontrol, billeder, tilgængelighed og native platformprøver | Ingen dokumenteret manuel afprøvning. | macOS, Windows og Linux samt temaer, skalering og skærmlæser skal afprøves. |
