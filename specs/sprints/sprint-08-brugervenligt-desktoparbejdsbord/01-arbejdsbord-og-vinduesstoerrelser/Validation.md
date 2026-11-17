# Validering: Tilpas arbejdsbordet til minimumsstørrelsen

Status: **automatiseret kontrol udført; manuel validering udestår**. Headless-kontrol dokumenterer ikke native vinduers udseende eller tilgængelighed.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Skift gentagne gange mellem minimum, standard og stort vindue, og fold kontekst til og fra.
2. Brug lange navne og filstier; kontrollér statuslinje og topbjælke med tastatur.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Panelbredder kan blive ugyldige ved størrelsesskift. Kontrollér genåbning og at skjulte kolonner ikke optager plads. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 17. november 2026, macOS, headless | Bygning og automatiserede prøver | `dotnet restore` og `dotnet build` uden fejl; 628 bestået, 6 oversprunget. Regressionen kontrollerer åbning/lukning ved 960 × 600, kolonnebredder og bevarelse af personvalg og ugemt editorbuffer ved størrelsesskift. | Headless erstatter ikke native prøver. |
| Ikke udført | R1-01–R1-03, visuel kontrol og tilgængelighed | Ingen native før-/efterbilleder eller manuelle platformprøver. | Kontrollér macOS, Windows og Linux, temaer, skalering, høj kontrast, tastatur og skærmlæser; mål og justér eventuelt brudpunktet. |
