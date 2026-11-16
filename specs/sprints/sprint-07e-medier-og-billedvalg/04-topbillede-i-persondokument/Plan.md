# Plan: Ét topbillede pr. persondokument

Status: **planlagt**. Estimat: **2–3 udviklingsdage**. Afhænger af feature: 03.

Lad brugeren vælge, skifte og fjerne ét billede øverst i personens dokument. Valget skal leve i dokumentet og overleve alle eksisterende opdateringsveje.

Kontrakter: [Mediekontrakt.md](../Mediekontrakt.md). Krav: [Requirements.md](Requirements.md). Validering: [Validation.md](Validation.md).

## Implementeringstrin

1. Implementér parser/validering og rene operationer for topbilledblokken i `BiographyImageEditService` i Core. Brug præcis kontrakten i Mediekontrakt.md og skriv fejlende tests først.
2. Tilføj kommandoerne Vælg topbillede, Skift topbillede og Fjern topbillede. Knapperne afhænger af en gyldig blok og en aktiv redigerbar person; vis en forståelig fejl ved ødelagt blok.
3. Åbn den fælles vælger med passende formål. Ved retur kontrolleres kontekst, filstatus og bufferrevision igen. Ny blok placeres først i body; udskiftning berører kun eksisterende blok.
4. Anvend ændringen på aktuel editorbuffer og behold metadata uændret. Implementér den fælles tekstfeltadapter i View til bufferudskiftning som én redigeringshandling. Opdatér dirty-state, preview og markør gennem adapteren; understøt én samlet Fortryd/Gentag.
5. Gennemgå `BiographyGeneratedSectionMerger`, `BiographyDocumentUpdater`, `BiographyTemplateDocumentUpdater` og MainWindow-kandidatvejene. Portrætpræfiks skal bevares ved opdatering, også når editoren er dirty. En gammel kandidat må ikke overskrive nyere billedvalg; genberegn eller afvis ud fra revision.
6. Tilføj korte hjælpetekster om placering og at originalfilen bliver i mediemappen. Et billede må ikke vælges automatisk ved import eller personsøgning.

## Afslutningskriterium

Alle featurekrav er dækket af relevante tests, det manuelle flow er gennemført, og resultaterne er skrevet i Validation.md. Fejlende tests skrives før adfærdsændringer i stiopslag, tekstoperationer og kandidatbevaring. Featureen må ikke erklæres færdig alene på baggrund af UI-skærmbilleder.
