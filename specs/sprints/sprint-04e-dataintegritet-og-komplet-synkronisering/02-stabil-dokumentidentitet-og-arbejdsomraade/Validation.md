# Validation: Stabil dokumentidentitet og levende arbejdsområde

- [x] En GEDCOM-navneændring bevarer samme fil og fri tekst.
- [x] Der oprettes ikke en ekstra fil med samme `recordId`.
- [x] Ikke-matchede dokumenter forbliver synlige efter flere importer i samme session.
- [x] Dublerede record-id'er vises som tvetydige og ændres ikke automatisk.
- [x] Kataloget opdateres uden genstart efter oprettelse og migrering.
- [x] Mappeskift håndterer dirty editorer eksplicit.
- [x] Ingen fil i gammel mappe ændres efter aktivering af ny mappe.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Manuel test med navneændring og to arbejdsmapper er gennemført.

## Automatiseret verifikation

- **Dato:** 2026-08-11
- **Build:** `dotnet build --no-restore -m:1 /nodeReuse:false` — 0 fejl og 0 advarsler.
- **Tests:** `dotnet test --no-build --no-restore -m:1 /nodeReuse:false` — 120 bestået, 0 fejlet.
- **Dækkede forløb:** navneændring med stabil filsti, dubletblokering, to importer i samme session, ikke-matchede dokumenter samt mappeskift med Gem, Kassér og Annullér.

## Manuel godkendelse

- **Dato:**
- **Godkendt af:**
- **Build eller commit:**
- **Bemærkninger:**

- [ ] Feature 02 er godkendt, og feature 03 må påbegyndes.
