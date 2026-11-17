# Validering: Ret temaressourcer og statusflader

Status: **delvist udført**. Automatiseret kontrol og beregning er udført; manuel og native afprøvninger udestår.

## Automatiseret kontrol

Kør `dotnet restore`, `dotnet build` og `dotnet test` fra projektroden. Registrér antal beståede, fejlende og oversprungne prøver. Tilføj meningsfulde regressioner for ændret adfærd og tilstandsovergange i `tests/SlaegtsAssistent.App.Tests`; brug FluentAssertions. Rene tekst-/stilrettelser kræver ikke tests, der blot gentager implementeringen.

## Manuel kontrol

1. Mål tekst og markeringers kontrast i begge temaer og høj kontrast; gem måling og skærmbilleder.
2. Prøv lange difflinjer, valgt tekst, tomme statusser og billedvælgerens miniaturebaggrunde.
3. Gentag med lyst/mørkt/systemtema, lange tekster, minimumsvindue, 100 %/200 % skalering og høj kontrast. Brug kun tastatur og kontrollér skærmlæserens navne, rækkefølge og status. Dokumentér native macOS-, Windows- og Linux-resultater særskilt.
4. Tag før-/efterbilleder ved minimum og standardstørrelse. Billederne skal demonstrere sektionens acceptkriterier i [Requirements.md](Requirements.md); notér størrelse, tema, skalering og datasæt.

## Accept og regression

Farveændringer kan gøre diff eller markering utydelig. Kontrollér både tekst og markering før gamle ressourcer fjernes. Kontrollér, at fri tekst, personvalg, ugemte buffers og annullering fortsat virker, hvor flowet berører dem. Brug syntetiske data og et midlertidigt arbejdsområde. Afslut først sektionen, når hvert krav har dokumenteret resultat; manglende platformadgang registreres som ikke prøvet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 2026-11-17, macOS | Restore, build og test | Restore og build bestået; samlet testkørsel: 646 bestået, 1 fejlet, 6 oversprunget. Den fejlede grafgenvejsprøve bestod ved særskilt gentagelse. De to nye prøver bestod igen efter sidste ændring. | Grafprøven kan være ustabil; de oversprungne eksportprøver blev ikke kørt. |
| 2026-11-17 | Tekstkontrast, beregnet fra ressourcefarver | Lys: 13,73–14,10:1; mørk: 10,13–11,27:1 for tekst på de nye status- og diffbaggrunde. | Kontrol- og fokusmarkeringer samt faktisk rendering er ikke målt. |
| 2026-11-17 | Diff uden farve | Plus og minus samt synlig forklaring viser betydningen, også hvis baggrundsfarver ikke kan skelnes. | Reservevisningen og systemets højkontrast skal prøves native. |
| Ikke udført | Manuel kontrol, billeder og native platformprøver | Ingen dokumenteret manuel afprøvning. | macOS, Windows og Linux, temaer, skalering, tastatur og skærmlæser udestår. |
