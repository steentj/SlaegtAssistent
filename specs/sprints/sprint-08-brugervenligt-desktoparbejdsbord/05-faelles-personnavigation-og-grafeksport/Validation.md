# Validering: Saml navigation og skjul grafeksportens detaljer

Status: **automatiseret kontrol udført; manuel og native platformprøver udestår**.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Afprøv tom graf, dublerede navne, valgt person uden dokument, stort netværk og personsøgning med tastatur.
2. Eksportér hel graf og udsnit i hvert format; annullér under eksport og kontrollér 960 × 600.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Samling af personvalg kan skabe rekursive opdateringer eller utilsigtede faneskift. Genbrug eksportmetoder og stabile person-id’er. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 17. november 2026, macOS headless | Bygning og automatiserede test | `dotnet restore` og `dotnet build --no-restore` lykkedes; `dotnet test --no-restore`: 640 bestået, 0 fejl, 6 oversprunget. Efterfølgende målrettet test af Enter i personlisten og eksportdialogen bestod. Test dækker personvalg i begge retninger, ugemt tekst, formatvalg, relevante indstillinger og annullering. | Ikke en native visuel prøve. |
| Ikke prøvet | R5-01–R5-03, native macOS, Windows og Linux | Manuel afprøvning af alle eksportformater og udskrift, tastatur, skærmlæser, temaer og billeder udestår. | Accept afventer dokumenteret platformprøve. |
