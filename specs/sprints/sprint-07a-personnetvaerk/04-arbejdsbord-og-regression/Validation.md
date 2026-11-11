# Validering: Arbejdsbord, proband og regression

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Integration: dirty A → klik C → A; kontrollér tekst, dirty-status, faneskift og uændret proband.
- Integration: skift proband, genstart uden original GEDCOM, skift arbejdsområde mens layout kører, og slet probanden.
- Regression: personlinks, gem alle, lukkeadvarsel, filtre, kandidatgodkendelse og skabeloneditor.
- Samlet: kør sprintets Validation.md og build/test; registrér platforme, som endnu ikke er fysisk afprøvet.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | G-09, G-10, G-11 samt samlet G-01–G-12 | Ikke registreret | Afventer implementering | Ingen accept endnu |
| 11. november 2026 | Del af G-09–G-10 | `dotnet test --no-restore` | 146 Core-tests og 220 App-tests bestået | En foreløbig graffane er tilsluttet. Automatisk opdatering, probandpersistens, visuel test, ydelse og platformspakning mangler; selve grafvisningen er ikke afprøvet med Graphviz. |
| 11. november 2026 | Del af G-09–G-10 | `dotnet test --no-restore`; lokal `sfdp -Tplain` | 147 Core-tests og 225 App-tests bestået; lokal Graphviz-prøve bestået | Graphviz og biblioteker er pakket lokalt til macOS arm64. Søgning og probandtekst er testet headless. Manuel visuel retest og reproducerbar pakning til alle platforme mangler. |
