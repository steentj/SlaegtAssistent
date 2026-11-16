# Plan: Fælles billedvælger med miniaturevisninger

Status: **planlagt**. Estimat: **3–4 udviklingsdage**. Afhænger af feature: 01, 02.

Byg én genanvendelig billedvælger, som de to indsættelseshandlinger kan anvende. Featureen kan afprøves med en midlertidig testvært, før dokumentændringer tilsluttes.

Kontrakter: [Mediekontrakt.md](../Mediekontrakt.md). Krav: [Requirements.md](Requirements.md). Validering: [Validation.md](Validation.md).

## Implementeringstrin

1. Opret UI-uafhængige `MediaCatalogItem`, `MediaSelectionRequest` og `MediaSelectionResult` samt `IMediaCatalogService` og `IMediaPickerDialogService`. Resultatet indeholder valgt filreference, alternativ tekst og valgfri billedtekst; intet skrives på disk.
2. Byg katalog fra godkendte snapshots, med personfilter som standard og unionen Alle refererede medier som alternativ. Genbrug opløseren fra 01. Indlæs ikke rå, afviste GEDCOM-ændringer som godkendte medier.
3. Implementér `MediaPickerViewModel`, `MediaPickerWindow.axaml` og `AvaloniaMediaPickerDialogService`. Genbrug designressourcer, modal ejer, tastaturfokus og danske tilgængelighedsnavne fra eksisterende dialoger.
4. Tilføj `IMediaThumbnailService` med UI-uafhængige forespørgsler/status. App-visningslaget ejer Avalonia-bitmaps og konvertering fra miniaturedata; ingen bitmap- eller kontroltyper i ViewModel.
5. Implementér sidevisning, søgning, markering og større forhåndsvisning, højst to jobs og begrænset cache. Kontroller format/signatur/dimensioner før dekodning og respekter EXIF. Frigiv ressourcer ved side-/persons-/kontekstskift og lukning.
6. Tilføj tomme tilstande, Opdatér, statustekst og indstillingskommando. En fejlende fil blokerer ikke resten. Annullering og forældede baggrundsresultater håndteres eksplicit.
7. Registrér services i `App.axaml.cs`; test via fakes og en headless-vært. De endelige knapper i hovedvinduet tilsluttes i 04/05.

## Afslutningskriterium

Alle featurekrav er dækket af relevante tests, det manuelle flow er gennemført, og resultaterne er skrevet i Validation.md. Fejlende tests skrives før adfærdsændringer i stiopslag, tekstoperationer og kandidatbevaring. Featureen må ikke erklæres færdig alene på baggrund af UI-skærmbilleder.
