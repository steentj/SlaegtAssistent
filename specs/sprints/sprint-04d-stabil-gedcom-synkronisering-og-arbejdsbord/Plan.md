# Plan: Sprint 04D – stabil GEDCOM-synkronisering og arbejdsbord

## Formål

Sprintet retter fem tæt forbundne problemer i import- og redigeringsflowet:

- Første import skal markere nye personer som nye uden at skabe falske ændringer.
- Genindlæsning af uændret GEDCOM skal være et no-op.
- Rå GEDCOM-data skal være tilgængelige efter genåbning.
- Midterste og højre arbejdsbordspanel skal kunne resizes.
- Skabelon- og GEDCOM-ændringer skal kunne genrenderes sikkert i markerede sektioner.

## Feature-rækkefølge

1. Korrekt importstatus og synkroniseringsbaseline.
2. Lokal persistens af GEDCOM-snapshot og rå personsegmenter.
3. Resizable arbejdsbordspaneler.
4. Kandidatbaseret genrendering ved ændret skabelon eller GEDCOM.

## Fælles arbejdsregler

- Skriv failing tests før implementering af hver feature.
- Brug en deterministisk, versionsstyret baseline; sammenlign ikke kun synlig Markdown-tekst.
- Bevar eksisterende fri tekst og AI-tekst.
- Hver feature afsluttes med målrettede tests og manuel validering.
- Stop efter hver feature. Næste feature må først starte efter brugerens manuelle godkendelse.
