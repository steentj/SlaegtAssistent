# Plan: Eksempeldata og live preview

1. Definér et lille deterministisk dansk eksempel med indhold i alle understøttede blokke.
2. Skriv Core-tests for eksempeldata og App-tests for datakildevalg, automatisk opdatering, cancellation og fejlbevarelse.
3. Implementér en previewservice, der kompilerer editormodellen og genbruger produktionsrenderingen.
4. Før resultatet gennem `SafeMarkdownPreviewService` med samme sikkerhedspolitik som hovedvinduet.
5. Tilføj valg mellem “Eksempelperson” og indlæste personer uden at kopiere eller gemme brugerdata i skabelonen.
6. Implementér debounce og latest-result-wins, så forældede renderinger ikke overskriver nyere preview.
7. Map compilerfejl til blok-id, når metadata gør det muligt; bevar ellers fil-, linje- og kolonnediagnostik.
8. Sammenlign manuelt editorpreview med hovedvinduets preview for samme person og model.
