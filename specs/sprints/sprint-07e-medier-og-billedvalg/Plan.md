# Plan: sprint 7E – medier og manuelt billedvalg

Status: **planlagt**. Der er kun udarbejdet specifikationer; ingen produktionskode er ændret.

## Mål

Brugeren kan angive en separat mediemappe, vælge ét topbillede til et persondokument og indsætte billeder ved markøren i livsforløbet gennem en fælles miniaturevælger. Den genererede medieoversigt vises som liste eller tabel og indsætter ingen billeder automatisk.

Læs [analysen](Analyse.md), [mediekontrakten](Mediekontrakt.md), [de samlede krav](Requirements.md) og [valideringsplanen](Validation.md) før implementering. Kontrakten fastlægger stier, dokumentejerskab, markører, samtidighed og ressourcebudgetter.

## Forudsætninger

- Eksisterende dokumentkatalog, godkendte snapshots, genererede markører, kandidatopdateringer og atomisk gemning fungerer.
- Billedpreview og biografieksport fra sprint 7C anvendes som integrationsgrundlag. Åbne fejl og faktisk platformstatus noteres før start; planens eksistens betyder ikke, at tidligere sprints er godkendt.
- Versionsintegration forudsætter den implementerede del af sprint 7D. Billedindsættelse kræver ikke aktiveret Git.
- Arbejdet udføres på syntetiske filer og midlertidige arbejdsområder. Brugerens eksisterende dokumenter må ikke migreres som led i udviklingstest.

## Leverancer og rækkefølge

| Nr. | Feature og detaljeret plan | Estimat | Afhængighed | Reviewbar leverance |
| --- | --- | --- | --- | --- |
| 01 | [Mediemappe og stiopslag](01-mediemappe-og-stiopslag/Plan.md) | 3–4 dage | Eksisterende indstillinger | Gem/genåbn mappe, opløs reference entydigt og brug samme rødder i appen. |
| 02 | [Liste og tabel](02-medieoversigt-som-liste-og-tabel/Plan.md) | 1–2 dage | 01 | Genereret medieoversigt uden automatisk billedsyntaks. |
| 03 | [Miniaturevælger](03-billedvaelger-med-miniaturer/Plan.md) | 3–4 dage | 01–02 | Afprøvbar dialog med søgning, filtre, miniaturevisninger og fejlstatus. |
| 04 | [Topbillede](04-topbillede-i-persondokument/Plan.md) | 2–3 dage | 03 | Vælg/skift/fjern ét billede, gemt i Markdown og bevaret ved opdatering. |
| 05 | [Livsforløbsbilleder](05-billeder-i-livsforloeb/Plan.md) | 2–3 dage | 04 | Indsæt billede ved markør med fortryd og beskyttelse af faktaområdet. |
| 06 | [Eksport og afslutning](06-eksport-regression-og-afslutning/Plan.md) | 4–5 dage | 05 | Verificeret samlet brugerflow, eksport, platforme, hjælp og regression. |

Samlet: **15–21 effektive udviklingsdage**, omtrent tre til fire arbejdsuger for én udvikler. Kalenderbuffer til accept og platformadgang er ikke indregnet. Hvis tidsboksen er ti dage, afsluttes første leverance efter 03; 04–06 udgør anden leverance. Den samlede funktionalitet kræver begge.

## Implementeringsstrategi

1. Registrér baseline med `dotnet restore`, `dotnet build` og `dotnet test`. Notér eksisterende fejl særskilt.
2. Implementér features i rækkefølge. Skriv først regressioner for stiopslag, beskyttede tekstområder, kandidatbevaring og eksport; implementér derefter adfærden. Undgå nye tredjepartsafhængigheder, medmindre billedafkodningen dokumenteret kræver dem.
3. Gennemfør hver features manuelle validering og registrér resultater, før den næste afsluttes. Brug tidlige native prøvevinduer til miniaturevisning og fortryd, så problemer opdages før samlet accept.
4. Brug eksisterende designressourcer og services til mappevalg/dialoger. Hold ViewModels uden Avalonia-typer. Nye services registreres i composition root, og nye serialiserede typer medtages i JSON-kontekster, hvis sådanne bliver nødvendige.
5. Afslut med feature 06 og hele [Validation.md](Validation.md). Status ændres først til gennemført efter dokumenteret samlet accept; manglende platformprøver beskrives eksplicit.

## Vigtige produktvalg

- Mediemappen er global og valgfri; et eksisterende setup fungerer uden at udfylde den.
- Vælgeren viser personens godkendte medier først, med mulighed for alle refererede medier i arbejdsområdet.
- Topbilledet ligger før den genererede sektion, normalt før titlen. Det er et brugervalg og påvirkes ikke af medieblokkens synlighed eller skabelonrækkefølge.
- Billeder i livsforløbet ligger i den frie tekst og indsættes kun dér, hvor brugeren har placeret markøren.
- Første version vælger PNG/JPEG. Andre medietyper er fortsat med i oversigten med forståelig status.
- Billedfilerne bliver i mediemappen. Relative henvisninger versioneres med Markdown; flytning eller sletning af originaler håndteres ikke af Git-historikken.

## Færdigkriterier

Alle krav i Requirements.md har automatiseret eller manuel evidens, samtlige seks featurejournaler er udfyldt, og hovedscenariet består uden ny GEDCOM-import efter genstart. PDF, DOCX og ODT er både strukturelt og visuelt kontrolleret. Ingen automatisk billedindsættelse eller tab af fri tekst ved import, genrendering, gemning eller rollback. Ressourcegrænser og målplatformenes native adfærd er verificeret; uafsluttede prøver må ikke tælles som bestået.

Ingen implementering, skærmbilleder af ny UI eller beståede tests påstås i denne planlægningsleverance.
