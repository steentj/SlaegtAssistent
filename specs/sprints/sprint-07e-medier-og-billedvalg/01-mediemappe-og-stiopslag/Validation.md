# Validering: Mediemappe og entydigt stiopslag

Status: **delvist implementeret og automatisk prøvet; ikke godkendt**. Indekset bruges ved import; fælles kontekst og manuel validering mangler.

## Automatiserede prøver

- Core: relativ undermappe, omvendte separatorer, original absolut sti, fremmed drev, ombaseret absolut sti, entydigt filnavn, dubletfilnavn, længste entydige stiafslutning og utilgængelig del af indeks.
- Filsystemintegration: symlink ud af rod, symlinksløjfe, `../` ud af tilladt rod, UNC, URL, `file:`-URI, ugyldige tegn, æøå, mellemrum, `#`, `%`, parenteser og forskelligt drev.
- App: gammel/ny JSON, Gem, Gem som, Annullér, normalisering, atomisk skrivefejl med tilbagerulning og en gemt mappe, der senere mangler.
- Åben dirty editor: skift mediemappe, behold teksten og dirty-status, invalidér preview og fjern tidligere rod fra tilladelserne.

Assertions bruger FluentAssertions. Regressioner skal påvise de konkrete krav i [Requirements.md](Requirements.md), inklusive fejlsituationer; undgå tests, der blot gentager implementationen.

## Manuel prøve

1. Opret syntetiske mapper GEDCOM, Medier og Dokumenter som søskende; læg kun billeder i Medier.
2. Vælg mediemappen, Gem, genstart og kontrollér felt og mediestatus uden ny GEDCOM-import.
3. Afprøv et gammelt Windows-filnavn, en dublet i to undermapper og en manglende fil; kontroller deterministiske forklaringer.
4. Skriv ugemt tekst, skift mediemappe og annullér derefter en anden ændring. Teksten skal være intakt, og ingen Markdown-fil må være skrevet.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 16. november 2026, macOS | Indstillinger, direkte opslag, symbolske links og previewrødder | Automatiske regressioner kørt med `dotnet test` | Fuld kontekstintegration og manuel prøve mangler. |
| 16. november 2026, macOS | Indeks og import | Automatiske prøver for længste stiafslutning, dubletter, afbrydelse, ufuldstændigt indeks og symbolsk mappe | Øvrige adgangsveje og manuel prøve mangler. |

| 16. november 2026, macOS | Genbrug af medieindeks | Automatisk prøve for genbrug og eksplicit invalidering | Filsystemets casefølsomhed og øvrige platforme mangler validering. |
