# Validering: Forenkling af forhåndsvisning og dansk sprog

Status: **automatiseret kontrol udført; manuel validering godkendt af brugeren**.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Prøv normal biografi, blokeret ressource, tomt dokument og skift frem og tilbage fra rå HTML.
2. Kontrollér danske tekster ved minimumsbredde, skalering og skærmlæser.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Flytning af visningsvalg kan skjule sikkerhedsstatus. Bevar eksisterende bindinger og renderingsservice. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 17. november 2026 / macOS | Restore, build og test | Restore og build bestået; 647 tests bestået, 0 fejl, 6 oversprunget. Test af tilstandsskift og eksisterende sikkerhedstests bestået. | Native visuel kontrol, tastatur, skærmlæser, temaer, skalering og billeder er ikke prøvet. Windows og Linux er ikke prøvet. |

| 17. november 2026 / platform ikke oplyst | Manuel validering | Brugeren har bekræftet, at den manuelle validering er i orden. | Platforme, konkrete prøver og billeder er ikke specificeret. |
