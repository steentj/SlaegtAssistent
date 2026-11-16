# Validering: Fælles billedvælger med miniaturevisninger

Status: **ikke kørt; featureen er ikke implementeret**.

## Automatiserede prøver

- ViewModel med fakes: personfilter, alle medier, søgning, sidegrænse, én markering, alt-tekst/billedtekst, tom tilstand og knappens CanExecute.
- Katalog efter genstart fra Approved-snapshot; inline-medier uden id; samme fil hos to personer; samme titel med forskellige filer; afviste importmedier kommer ikke med.
- Rigtige syntetiske filer: PNG/JPEG, EXIF-roteret JPEG, korrupt fil, falsk filendelse, for stor fil og for mange pixels. Andre formater står som ikke-valgbare.
- Annullering under indeksering/dekodning, hurtigt filterskift og luk/genåbn. Forældede jobs må ikke opdatere nyt indhold.
- Headless: tilgængelige navne, Tab/Enter/Esc, valgt kort og disabled indsættelse. Native afprøvning af faktisk billedvisning på målplatforme.

Assertions bruger FluentAssertions. Regressioner skal påvise de konkrete krav i [Requirements.md](Requirements.md), inklusive fejlsituationer; undgå tests, der blot gentager implementationen.

## Manuel prøve

1. Åbn testværten med 250 referencer og mindst fire fejltyper. Søg og skift side mens miniaturer indlæses.
2. Kontrollér alle fejlstatusser og at en manglende/ødelagt fil ikke kan vælges.
3. Kontrollér personfilter og Alle refererede medier, større preview, EXIF og alternativ tekst alene med tastatur.
4. Kør ydelsesprøven fra sprintets Validation.md og dokumentér målinger og cacheadfærd.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| Ikke kørt | Alle | Afventer implementering | – |
