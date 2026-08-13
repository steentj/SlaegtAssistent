# Validation: Komplet kanonisk synkroniseringsbaseline

- [x] Alle understøttede strukturerede datatyper indgår i baseline.
- [x] Ikke-betydende rækkefølge ændrer ikke fingerprint.
- [x] Betydende rækkefølge bevares.
- [x] Et felt, der ikke renderes af skabelonen, kan stadig give ændringsstatus.
- [x] Importeret, godkendt og dokumentafledt tilstand er adskilt.
- [x] Manglende og ukendt baselineversion vises tydeligt.
- [x] Uændret genimport er et no-op.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Manuel baselineprøve for alle hoveddatatyper er gennemført.

## Automatiseret resultat

- **Dato:** 2026-08-13
- **Målrettet Core-suite:** `CanonicalBiographySnapshotTests`, 19 tests bestået.
- **Målrettede App-forløb:** identisk no-op, skjult felt, manglende baseline og ukendt baselineversion er bestået.
- **Core-suite:** 92 tests bestået.
- **App-suite:** 97 tests bestået.
- **Samlet:** 189 tests bestået, 0 fejlet.
- **Build:** bestået med 0 advarsler og 0 fejl.

## Foreslået manuel prøve

1. Importér `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/complete-gedcom-551.ged` i et tomt testarbejdsområde, og gem eventuelle åbne editorer.
2. Notér ændringstid og indhold for Markdown-filerne og `.slaegtsassistent/gedcom/manifest.json`.
3. Importér samme fil igen. Kontrollér status **Færdig – ingen ændringer**, tom forskelsdialog og byte-identiske filer med uændrede ændringstider.
4. Lav en kopi af GEDCOM-filen, og ændr kun Annas `NOTE`. Importér kopien.
5. Kontrollér, at appen viser kandidaten **Synkroniseringsbaseline**, selv om noten ikke fremgår af standardskabelonen.
6. Afvis kandidaten, og kontrollér, at dokument og godkendt baseline forbliver uændrede.
7. Gentag enkeltvis med en ændring i personhændelse, familiehændelse, census, kildecitation, medie og submitter. Kontrollér, at hver ændring giver gennemgang.
8. Godkend en kandidat, gem dokumentet og genimportér samme fil. Kontrollér derefter status **Færdig – ingen ændringer**.
9. Åbn et formatversion 2-dokument uden `syncBaseline`, og kontrollér teksten **Baseline mangler** samt manuel migrering.
10. Sæt baselineversionen til `99` i en testkopi, og kontrollér teksten **Ukendt baselineversion** samt at filen ikke ændres automatisk.

## Manuel godkendelse

- **Dato:** 2026-08-13
- **Godkendt af:** Produktejer
- **Build eller commit:** Lokalt arbejdsområde, 189 tests bestået
- **Bemærkninger:** Feature 4.8.7 godkendt efter manuel prøve.

- [x] Feature 07 er godkendt, og feature 08 må påbegyndes.
