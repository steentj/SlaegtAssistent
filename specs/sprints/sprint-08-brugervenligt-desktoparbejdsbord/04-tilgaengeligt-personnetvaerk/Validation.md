# Validering: Gør grafens information tilgængelig uden mus

Status: **automatiseret kontrol udført; manuel og native platformprøver udestår**.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Gennemfør hele personflowet alene med tastatur; kontrollér kamera og fokus efter åbning og tilbagevenden.
2. Prøv VoiceOver på macOS og Narrator på Windows samt relevant Linux-skærmlæser ved platformaccept.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Hover, valg og handlingsmål kan komme ud af takt. Bevar eksisterende klik til biografi, kameramatematik og layoutmotor. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 17. november 2026, macOS headless | Bygning og automatiserede test | `dotnet restore` og `dotnet build --no-restore` lykkedes; `dotnet test --no-restore`: 636 bestået, 0 fejl, 6 oversprunget. Test dækker stabilt personvalg under hover, forbindelsestyper, kommandoers tilstand, tastaturpanorering, zoom, nulstilling og automation peer. | Ikke en native visuel prøve. |
| Ikke prøvet | R4-01–R4-03, native macOS, Windows og Linux | Manuel afprøvning af tastaturflow, skærmlæser, temaer og billeder udestår. | Accept afventer dokumenteret platformprøve. |
| 17. november 2026, macOS headless | Rettelse af tastatur i hovedvinduet | Graffanens fokus, højre pil, plus-/minustegn og piletast i søgefeltet er afprøvet i fanekontrollen; samlet testkørsel: 637 bestået, 0 fejl, 6 oversprunget. | Native afprøvning af dansk tastaturlayout udestår. |
