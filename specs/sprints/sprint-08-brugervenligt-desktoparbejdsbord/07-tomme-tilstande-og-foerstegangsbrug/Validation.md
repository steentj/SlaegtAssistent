# Validering: Tilføj handlingsrettede tomme tilstande

Status: **automatiseret kontrol udført; manuel validering afventer**.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Start med tom dokumentmappe, med eksisterende dokumenter og med søgning uden træffere.
2. Gennemfør startaktionen og rydning af søgning med tastatur og skærmlæser.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Asynkron indlæsning kan vise forkert tomtilstand. Test overgangene; tilføj ikke eksempeldata som skjult sideeffekt. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 17. november 2026 / macOS | Restore, build og test | Restore og build bestået; 644 prøver bestået, 0 fejlede, 6 oversprunget. Regressioner dækker tomt arbejdsområde, fejl, import og søgefilter. | Native visning, tastatur, skærmlæser, temaer, skalering og billeder er ikke prøvet. |
| Windows og Linux | Manuel kontrol | Ikke prøvet | Afventer platformadgang. |
