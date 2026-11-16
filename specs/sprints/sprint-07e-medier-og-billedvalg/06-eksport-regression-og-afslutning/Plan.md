# Plan: Eksport, platforme og samlet accept

Status: **planlagt**. Estimat: **4–5 udviklingsdage**. Afhænger af feature: 05.

Afslut funktionaliteten som et sammenhængende lokalt brugerflow med dokumenteret kvalitet i preview, delingsformater og versionshistorik.

Kontrakter: [Mediekontrakt.md](../Mediekontrakt.md). Krav: [Requirements.md](Requirements.md). Validering: [Validation.md](Validation.md).

## Implementeringstrin

1. Saml rodkontekst hos eksportkald, preflight og konvertering. Fasthold et snapshot gennem hele eksportjobbet og bevar eksisterende fingerprint-/ændringskontrol. Kontrollér, at preview og eksport ikke sender filstier til netværk.
2. Udvid syntetiske fixtures med separat mediemappe, topbillede, to livsforløbsbilleder, manglende fil og flere personer. Hold originaler uændrede og fjern cache fra versionsomfang.
3. Kør automatiske Core-, App-, headless- og eksisterende lokale eksportintegrationstests. Undersøg fejl mod baseline; ændr ikke uvedkommende funktionalitet for at skjule dem.
4. Generér PDF, DOCX og ODT, både enkeltvis og samlet samt fra ugemte buffers. Undersøg faktisk billedindlejring og visuel placering, skalering, billedtekster og sideskift. Printflow skal åbne en PDF med samme indhold.
5. Gennemfør native prøve af vælger og tekstfelt på macOS og Windows; Linux, hvis platformen fortsat distribueres. Verificér Native AOT på samme målplatforme og at alle nye serialiserede typer er kildegenererede.
6. Verificér Commit, diff og tilbagerulning af dokumenternes billedhenvisninger. Billedoriginalerne versioneres fortsat ikke; ingen implicit sletning eller ændring af originaler.
7. Opdatér README, Markdown-/skabelonhjælp, Mission.md og CHANGELOG med faktisk implementeret adfærd. Opdatér roadmapstatus først efter accept. Registrér resultater, afvigelser, ydelsesmålinger og eventuelle uafprøvede platforme i Validation.md.

## Afslutningskriterium

Alle featurekrav er dækket af relevante tests, det manuelle flow er gennemført, og resultaterne er skrevet i Validation.md. Fejlende tests skrives før adfærdsændringer i stiopslag, tekstoperationer og kandidatbevaring. Featureen må ikke erklæres færdig alene på baggrund af UI-skærmbilleder.
