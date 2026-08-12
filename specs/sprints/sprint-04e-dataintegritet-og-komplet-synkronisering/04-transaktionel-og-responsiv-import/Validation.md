# Validation: Transaktionel og responsiv import

- [x] Forhåndskontrollen foretager ingen vedvarende skrivninger.
- [x] Afvist eller annulleret import ændrer ingen filer, snapshot eller editorer.
- [x] Fejl i hver fase efterlader en dokumenteret konsistent tilstand.
- [x] Import kan ikke køre parallelt i samme arbejdsområde.
- [x] UI er responsivt under parsing og hashing.
- [x] Importfase, annullering og fejl er tydelige og på dansk.
- [x] Snapshot, katalog og personliste skifter generation samlet.
- [x] `dotnet build` og `dotnet test` er grønne.
- [x] Manuel annullerings- og fejltest er gennemført.

## Automatiseret verifikation

- **Dato:** 2026-08-12
- **Build:** `dotnet build --no-restore -m:1 /nodeReuse:false` — 0 fejl og 0 advarsler.
- **Tests:** `dotnet test --no-build --no-restore -m:1 /nodeReuse:false` — 139 bestået, 0 fejlet.
- **Forhåndskontrol:** GEDCOM, katalog og alle genererede kandidater fortolkes uden vedvarende skrivninger.
- **Gennemgang:** annullering afbryder ventende dialog og efterlader filer, snapshot, personliste og valgt GEDCOM uændret.
- **Gennemførelse:** Markdown og `.slaegtsassistent` gendannes byte-for-byte ved fejl, inklusive et delvist skrevet snapshotmanifest.
- **Responsivitet:** parsing, kataloglæsning, skabelonrendering, hashing, filskrivning og rollback kører uden for UI-tråden; parseren modtager annullering pr. GEDCOM-post.
- **Publicering:** editorændringer, katalog, personliste, valgt GEDCOM og snapshot publiceres først efter gennemført commit.

## Manuel godkendelse

- **Dato:** 2026-08-12
- **Godkendt af:** Produktejer
- **Build eller commit:** Lokal arbejdsmappe, 139 automatiske tests bestået
- **Bemærkninger:** Manuel annullerings-, responsivitets- og rollbacktest er godkendt.

- [x] Feature 04 er godkendt, og feature 05 må påbegyndes.
