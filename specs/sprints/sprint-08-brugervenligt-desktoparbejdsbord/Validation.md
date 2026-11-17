# Validering: sprint 8 – brugervenligt desktoparbejdsbord

Status: **ikke implementeret; samlet accept ikke udført**.

## Historisk udgangspunkt

GUI-gennemgangen den 17. november 2026 gav en vellykket bygning uden advarsler samt 627 beståede og 6 oversprungne tests. Det er baseline fra før sprint 8, ikke accept af dette sprint. Appprocessen startede, men vinduet kunne ikke inspiceres. Ingen visuelle eller native tilgængelighedsprøver er dokumenteret som bestået.

## Samlet prøve

1. Kør `dotnet restore`, `dotnet build` og `dotnet test`; registrér resultater og begrund oversprungne tests.
2. Start med `cd src/SlaegtsAssistent.App && dotnet run` på syntetiske data i et midlertidigt arbejdsområde.
3. Find en person via fælles søgning, inspicér graf og forbindelser, åbn biografi, redigér og kontrollér forhåndsvisningen. Gentag alene med tastatur.
4. Indlæs ændret GEDCOM, gennemgå flere personer, annullér og kontrollér uændrede dokumenter. Gentag med selektiv godkendelse og kontrollér præcist resultat samt bevaret fri tekst.
5. Gem alle, opret en version, indsæt et billede og eksportér en ugemt biografi. Kontrollér eksisterende advarsler, annullering og at eksport ikke gemmer Markdown som sideeffekt.
6. Gentag ved 960 × 600, 1.220 × 760 og stort hovedvindue, samt hver dialogs minimum. Afprøv begge temaer, systemtema, lange tekster, store datasæt, høj kontrast og 100 %/200 % skalering.
7. Dokumentér native macOS med VoiceOver, Windows med Narrator og Linux med relevant skærmlæser. Kontrollér platformmenuer, genveje og dialogrækkefølge. Ikke tilgængelige platforme noteres som ikke prøvet.
8. Saml før-/efterbilleder, kontrastmålinger og sektionsjournaler. Kontrollér samtlige krav S8-01–S8-08 og R1–R10.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| Ikke udført | S8-01–S8-08 | Specifikation oprettet; ingen implementeringsaccept | Alle sektionsprøver og native prøver udestår |
