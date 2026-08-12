# Validation: Komplet GEDCOM 5.5.1-fortolkning

- [x] Mappingtabellen er komplet for produktets datakontrakt.
- [x] `CONT` og `CONC` bevares korrekt i noter, kilder og øvrige tekstfelter.
- [x] Person-, familie-, event- og censuscitationer bevarer alle aftalte underfelter.
- [x] Struktur- og relationstags vises ikke som hændelser.
- [x] Ukendte events bevares med rå tag og diagnostik.
- [x] UTF-8, Unicode little-endian, Unicode big-endian, ASCII og ANSEL er dækket af tests.
- [x] Danske tegn bevares uden lydløs erstatning.
- [x] Output og rækkefølge er deterministisk.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Manuel sammenligning med repræsentativ GEDCOM er gennemført.

## Automatiseret resultat

- **Dato:** 2026-08-12
- **Målrettet suite:** `GedcomCompletenessTests`, 14 tests bestået.
- **Core-suite:** 62 tests bestået.
- **App-suite:** 91 tests bestået.
- **Samlet:** 153 tests bestået, 0 fejlet.
- **Build:** bestået med 0 advarsler og 0 fejl.

## Foreslået manuel prøve

1. Start appen og vælg et tomt testarbejdsområde.
2. Importér `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/complete-gedcom-551.ged`.
3. Kontrollér, at Anna Jensen og Jens Hansen vises, og at de tilhører samme familie.
4. Kontrollér Annas fødsel, census og kilder samt familiens vielse i den genererede Markdown/preview.
5. Kontrollér, at `FAMC`, `FAMS` og `CHAN` ikke vises som hændelser.
6. Kontrollér, at den ukendte `_FLYT`-hændelse bevares og giver en synlig advarsel.
7. Genimportér samme fil og kontrollér, at rækkefølge og indhold er uændret, og at fri brugertekst ikke ændres.

## Manuel godkendelse

- **Dato:**
- **Godkendt af:**
- **Build eller commit:**
- **Bemærkninger:**

- [ ] Feature 05 er godkendt, og feature 06 må påbegyndes.
