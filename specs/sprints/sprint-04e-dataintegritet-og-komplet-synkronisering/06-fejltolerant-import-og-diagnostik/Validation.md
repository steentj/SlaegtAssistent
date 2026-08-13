# Validation: Fejltolerant import og synlige diagnostikker

- [x] Isolerbare postfejl blokerer ikke øvrige gyldige poster.
- [x] Fatale filfejl klassificeres og ændrer ikke arbejdsområdet.
- [x] Ingen kendt datadropping sker uden diagnostik.
- [x] Hver diagnostik viser relevante linje-, post- og tagoplysninger.
- [x] Importoversigten viser korrekte totaler og severity.
- [x] Delvis import kræver eksplicit accept.
- [x] Afvist delvis import efterlader filer og UI-state uændret.
- [x] `dotnet build` og `dotnet test` er grønne.
- [x] Manuel gendannelses- og fatalfejltest er gennemført.

## Automatiseret resultat

- **Dato:** 2026-08-12
- **Målrettede Core-tests:** `GedcomFaultToleranceTests`, 11 bestået.
- **Målrettede App-tests:** 4 tests for accept, afvisning, filtrering/navigation og fatal rapport bestået.
- **Core-suite:** 73 tests bestået.
- **App-suite:** 95 tests bestået.
- **Samlet:** 168 tests bestået, 0 fejlet.
- **Build:** bestået med 0 advarsler og 0 fejl.

## Foreslået manuel prøve

### Delvis import

1. Start appen med et testarbejdsområde, som allerede indeholder mindst én kendt Markdown-fil.
2. Importér `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/partial-recovery.ged`.
3. Kontrollér, at dialogen viser 2 importerede poster, 2 med diagnostik, 1 oversprunget og 0 fatale.
4. Vælg først **Afvis import**, og kontrollér, at filer, personliste, valgt GEDCOM og åbne editorer er uændrede.
5. Importér samme fil igen, og vælg **Fortsæt med delvis import**.
6. Kontrollér, at Anna Jensen og Bent Jensen importeres, mens posten uden record-id ikke gør.
7. Åbn **Importrapport**, filtrér mellem **Alle**, **Advarsler** og **Fejl**, og kontrollér besked, konsekvens, linje, tag og filsti.
8. Vælg diagnostikken for `@I1@` eller `@I2@`, og kontrollér, at personlisten navigerer til den relevante person.

### Fatal import

1. Importér `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/fatal-missing-trailer.ged`.
2. Kontrollér, at importen afbrydes uden acceptdialog og viser 0 importerede, 0 oversprungne og 1 fatal fejl.
3. Kontrollér, at arbejdsområdets filer, personliste, valgte GEDCOM og åbne editorer fortsat er uændrede.

## Manuel godkendelse

- **Dato:** 2026-08-13
- **Godkendt af:** Produktejer
- **Build eller commit:** Lokal arbejdsmappe, 168 automatiske tests bestået
- **Bemærkninger:** Delvis import, afvisning, accept, diagnostikfiltrering og fatal import er godkendt.

- [x] Feature 06 er godkendt, og feature 07 må påbegyndes.
