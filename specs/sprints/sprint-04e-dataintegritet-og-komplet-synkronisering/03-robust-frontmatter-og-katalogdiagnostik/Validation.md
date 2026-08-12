# Validation: Robust frontmatter, migrering og katalogdiagnostik

- [x] Én defekt fil blokerer ikke appstart eller øvrige dokumenter.
- [x] Dublerede nøgler og ugyldige værdier giver forståelige danske fejl.
- [x] Ukendt formatversion ændres ikke automatisk.
- [x] Understøttet migrering bevarer dokumentets body byte-for-byte.
- [x] Tvetydige record-id'er kan ikke matches eller overskrives automatisk.
- [x] Brugeren kan se filsti, fejlkategori og næste sikre handling.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Manuel katalogtest med mindst fire forskellige filtilstande er gennemført.

## Automatiseret verifikation

- **Dato:** 2026-08-12
- **Build:** `dotnet build --no-restore -m:1 /nodeReuse:false` — 0 fejl og 0 advarsler.
- **Tests:** `dotnet test --no-build --no-restore -m:1 /nodeReuse:false` — 134 bestået, 0 fejlet.
- **Dækkede tilstande:** gyldigt dokument, legacy-dokument uden frontmatter, dubleret nøgle, ugyldig værdi, manglende obligatorisk felt, ukendt version, understøttet ældre version og dubleret `recordId`.
- **Versionskontrakt:** version 2 er aktuel; version 0 og 1 kan tilbydes migreret; højere og negative versioner afvises uden skrivning.

## Manuel godkendelse

- **Dato:**
- **Godkendt af:**
- **Build eller commit:**
- **Bemærkninger:**

- [ ] Feature 03 er godkendt, og feature 04 må påbegyndes.
