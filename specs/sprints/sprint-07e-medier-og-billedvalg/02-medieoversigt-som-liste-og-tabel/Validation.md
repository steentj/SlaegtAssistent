# Validering: Medieoversigt uden automatiske billeder

Status: **delvist implementeret og automatisk prøvet; ikke godkendt**. Manuel prøve og de øvrige specificerede regressioner mangler.

## Automatiserede prøver

- Compiler og katalog: liste/tabel, skjult blok, tom liste, tomme felter, titel uden fil, fil uden titel, status for manglende fil, pipe/newline og Markdown-tegn i titel/sti.
- Alle automatisk genererede medier kontrolleres via Markdown-AST: ingen billednoder i medieoversigten.
- Tidligere v1-JSON med Standard åbnes, previewes og gemmes; inputfilen er uændret før Gem.
- Kandidataccept og afvisning i både GEDCOM- og skabelonopdatering; fri tekst og allerede manuelle billeder bevares.

Assertions bruger FluentAssertions. Regressioner skal påvise de konkrete krav i [Requirements.md](Requirements.md), inklusive fejlsituationer; undgå tests, der blot gentager implementationen.

## Manuel prøve

1. Importér en fixture med tre medier, herunder en manglende fil. Se listen og vælg derefter tabel i skabeloneditoren.
2. Kontrollér, at ingen af medierne vises som billeder ved ny generering.
3. Åbn et eksisterende dokument med automatisk medieafsnit, afvis en kandidat og kontrollér uændret fil; accepter derefter og kontrollér kun det genererede område.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 16. november 2026, macOS | Liste, tabel, tidligere Standard, import | Automatiske regressioner kørt med `dotnet test` | Øvrige scenarier og manuel prøve mangler. |
