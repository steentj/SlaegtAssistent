# Plan: Mediemappe og entydigt stiopslag

Status: **planlagt**. Estimat: **3–4 udviklingsdage**. Afhænger af feature: Ingen.

Tilføj en valgfri mediemappe og en fælles, kontrolleret vej fra GEDCOM-reference til lokal fil. Brug den eksisterende mappevælger og atomiske indstillingslagring.

Kontrakter: [Mediekontrakt.md](../Mediekontrakt.md). Krav: [Requirements.md](Requirements.md). Validering: [Validation.md](Validation.md).

## Implementeringstrin

1. Skriv fejlende tests for de nye sti- og indstillingsscenarier. Tilføj `MediaResolutionContext`, resultatstatus og `ILocalMediaFileSystem` i Core; implementér fysisk filadgang i App.
2. Udvid `BiographyMediaResolver` og indfør den fælles stipolitik efter Mediekontrakt.md. Bevar særskilte begreber for dokumentmappe og mediemappe i `BiographyTemplateContext` og generatorens konstruktør.
3. Tilføj `DefaultMediaFolder` til `AppSettings`, `SettingsWindowViewModel`, `SettingsWindow.axaml`, kildegenereret JSON og alle indstillingskopier i `MainWindowViewModel`. Gennemgå også Gem som, annullering og fejltilbagerulning.
4. Tilføj feltet **Mappe med GEDCOM-medier**, tekstfelt, **Vælg…** og hjælpeteksten: »Bruges til at finde filer, som GEDCOM henviser til. Billeder indsættes kun, når du vælger dem.« Tom værdi betyder eksisterende GEDCOM-relative opslag.
5. Kontrollér en ny ikke-tom mappe ved Gem. En ugyldig ny sti afvises med fejl ved feltet. En tidligere gemt mappe, som er blevet utilgængelig, bevares med advarsel og må ikke forhindre opstart eller ændring af tema.
6. Opret et fælles `MediaWorkspaceContext` i App, som leverer rødder og revision. Brug det ved importdiagnostik og skabelonkontekster. Byg et annullerbart, begrænset rodindeks til ombasering; cache pr. revision.
7. Gør editorens previewrødder opdaterbare og invalidér previewcache ved mediemappeskift. Tilføj mediemappen til eksportens fælles rodkilde allerede her; format-/layoutprøver afsluttes i feature 06. Ingen ændring af dokumenttekst eller dirty-status ved mappeskift.

## Afslutningskriterium

Alle featurekrav er dækket af relevante tests, det manuelle flow er gennemført, og resultaterne er skrevet i Validation.md. Fejlende tests skrives før adfærdsændringer i stiopslag, tekstoperationer og kandidatbevaring. Featureen må ikke erklæres færdig alene på baggrund af UI-skærmbilleder.
