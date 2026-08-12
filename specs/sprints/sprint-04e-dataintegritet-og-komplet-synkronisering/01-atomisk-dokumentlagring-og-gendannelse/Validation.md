# Validation: Atomisk dokumentlagring og gendannelse

- [x] Tests beviser, at gammel fil bevares ved fejl før commit.
- [x] Tests beviser, at en succesfuld commit giver den komplette nye fil.
- [x] Ingen produktionssti trunkerer en eksisterende brugerfil direkte.
- [x] Dirty-state bevares ved skrivefejl og nulstilles først ved succes.
- [x] Midlertidige filer og gendannelsesfiler håndteres deterministisk.
- [x] Fejl vises på dansk uden at lukke appen.
- [x] `dotnet build` og `dotnet test` er grønne.
- [x] Manuel fejltest på en dokumentkopi er gennemført.

## Automatiseret verifikation

- **Dato:** 2026-08-11
- **Build:** `dotnet build --no-restore -m:1 /nodeReuse:false` — 0 fejl og 0 advarsler.
- **Tests:** `dotnet test --no-build --no-restore -m:1 /nodeReuse:false` — 111 bestået, 0 fejlet.
- **Dækkede fejltrin:** oprettelse af midlertidig fil, flush, atomisk erstatning og fejl mellem kildekopi og manifest.

## Manuel godkendelse

- **Dato:** 2026-08-11
- **Godkendt af:** Produktejer
- **Build eller commit:** Lokal arbejdsmappe, 111 automatiske tests bestået
- **Bemærkninger:** Manuel fejltest gennemført med macOS-filflaget `uchg` på en dokumentkopi.

- [x] Feature 01 er godkendt, og feature 02 må påbegyndes.
