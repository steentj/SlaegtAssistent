# Validering: Indfør menuer og platformens genveje

Status: **automatiseret kontrol udført; manuel og native platformprøver udestår**.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Prøv genveje med fokus i biografitekst, personliste, skabelonfelt og hjælpevindue.
2. Kontrollér native menulinje på macOS og vinduesmenu på Windows/Linux; ingen dobbelt udførelse.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Globale genveje kan overtage tekstredigering eller udløse kommandoer to gange. Bevar native vinduesramme og eksisterende livscyklus. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 17. november 2026, macOS headless | Bygning og automatiserede test | `dotnet restore` og `dotnet build --no-restore` lykkedes; `dotnet test --no-restore`: 634 bestået, 0 fejl, 6 oversprunget. Menuens kommandoer, indstillingsgenvej og markering i fokuseret tekstfelt er testet headless. | Ikke en native visuel prøve. |
| Ikke prøvet | R3-01–R3-03, native macOS, Windows og Linux | Manuel afprøvning af genveje, skærmlæser, vinduesstørrelser og billeder udestår. | Accept afventer dokumenteret platformprøve. |
