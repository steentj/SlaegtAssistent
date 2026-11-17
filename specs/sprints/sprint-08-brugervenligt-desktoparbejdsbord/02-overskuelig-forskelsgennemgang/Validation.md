# Validering: Forenkling af forskelsgennemgangen

Status: **automatiseret kontrol udført; manuel validering udestår**. Headless-kontrol erstatter ikke native visuelle prøver.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Afprøv nye personer, manglende og ukendt baseline, skabelonændringer, lange feltværdier og mange forskelle.
2. Gennemgå alle ændringer med tastatur, skift person frem og tilbage, og sammenlign anvendt kandidat med valgene.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Dette er et sikkerhedskritisk flow. Regressioner skal dække beslutningsbevaring og annullering før UI-omlægning; Core-regler må ikke ændres. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 17. november 2026, macOS, headless | Bygning og automatiserede prøver | `dotnet restore` og `dotnet build` uden fejl; `dotnet test` gav 633 beståede og 6 oversprungne. Prøver dækker personskift, filtrering, kandidat pr. person, annullering, anvendte valg og afvist skabelon ved GEDCOM-import. | Headless erstatter ikke native prøver. |
| Ikke udført | R2-01–R2-03, visuel kontrol og tilgængelighed | Ingen native før-/efterbilleder eller manuelle platformprøver. | Kontrollér bredde, lange værdier, mange personer, temaer, skalering, høj kontrast, tastatur og skærmlæser på macOS, Windows og Linux. |
