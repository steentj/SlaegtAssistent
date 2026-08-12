# Plan: Sprint 04E – dataintegritet og komplet synkronisering

## Feature-rækkefølge

1. `01-atomisk-dokumentlagring-og-gendannelse`
2. `02-stabil-dokumentidentitet-og-arbejdsomraade`
3. `03-robust-frontmatter-og-katalogdiagnostik`
4. `04-transaktionel-og-responsiv-import`
5. `05-komplet-gedcom-fortolkning`
6. `06-fejltolerant-import-og-diagnostik`
7. `07-komplet-synkroniseringsbaseline`
8. `08-feltstyret-konfliktloesning`
9. `09-skabelonkontrakt-rendering-og-medier`
10. `10-privat-markdown-preview`
11. `11-regression-platform-og-distribution`

## Obligatorisk TDD-arbejdsgang for hver feature

1. Genlæs featurekravene og skriv en sporbar testliste for hvert acceptkriterium.
2. Skriv først én eller flere fejlende tests, der fejler af den forventede årsag.
3. Dokumentér den røde testkørsel kort i featureens arbejdsnoter eller commit.
4. Implementér den mindste komplette vertikale løsning, der får testene til at bestå.
5. Refaktorér uden at ændre observerbar adfærd og kør alle berørte tests efter hvert trin.
6. Kør `dotnet build` og hele `dotnet test`.
7. Gennemfør featureens manuelle scenarier med en separat testmappe og kopier af testdata.
8. Udfyld godkendelsesfeltet i `Validation.md`.
9. Stop arbejdet. Vent på brugerens skriftlige godkendelse, før næste feature påbegyndes.

## Fælles implementeringsregler

- Ingen test må omskrives til at acceptere en adfærd, der strider mod Missionen eller dette sprint, uden en udtrykkelig produktafgørelse.
- Fejlforløb må ikke efterlade snapshot, dokumenter, katalog og UI-tilstand i forskellige generationer.
- Testfakes for filsystem, ur og fejlindsprøjtning skal være deterministiske.
- Native filsystem- og UI-tests suppleres med platformsspecifikke grundlæggende funktionstest, hvor abstraherede tests ikke er tilstrækkelige.
- Manuel validering udføres på kopierbare testdata; produktionsdata må ikke bruges til destruktive fejlscenarier.

## Sprintafslutning

Sprintet er først færdigt, når alle 11 features er manuelt godkendt, hele testpakken er grøn, en genåbnet arbejdsmappe har bestået den samlede validering, og platform-/distributionsmatrixen er dokumenteret.
