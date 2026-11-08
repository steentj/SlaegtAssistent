# Requirements: Eksempeldata og live preview

- Et komplet lokalt eksempel skal kunne forhåndsvises uden GEDCOM-import.
- Eksemplet skal dække relationer, flere hændelser, folketælling, kilder, medieplaceholder og afsender.
- Hvis personer er indlæst, skal brugeren kunne vælge en af dem som previewgrundlag.
- Preview skal bruge samme compiler, loader, renderer og sikre HTML-pipeline som produktionsflowet.
- Preview skal opdateres automatisk efter en kort debounce og understøtte cancellation.
- En fejl må ikke rydde sidste gyldige preview.
- Fejl skal vises på dansk og så tæt som muligt på den relevante blok eller indstilling.
- Preview må ikke udføre scripts, ukontrolleret aktivt indhold eller netværkskald.
