# Plan: Personnavigation i editor og preview

## Løsningsvalg
Previewet får klikbare personnavne med en diskret ↗-markør via CSS. Over editorens almindelige tekstfelt vises en tastaturtilgængelig liste af knapper med de samme personmål. Vi bevarer teksteditorens normale markør, udklipsholder og undo-adfærd; syntetiske inline-elementer ville kræve en anden editor. Ingen markører indsættes i Markdown-filen.

## Implementering
1. Opløs relative Markdown-links i forhold til det aktive dokument og match kun entydige, fejlfri persondokumenter i det aktuelle katalog. Brug platformens stisammenligning og afvis eksterne, absolutte og ikke-katalogiserede mål.
2. Previewrenderingen erstatter godkendte personlinks med interne navigationsadresser og en CSS-markør. Den eksisterende blokering af øvrige lokale links og ressourcer bevares.
3. EditorViewModel eksponerer de fundne personmål og kommandoer. MainWindowViewModel validerer målet igen og skifter SelectedPerson gennem den eksisterende editorcache, så ugemte ændringer bevares. Et aktivt personfilter ryddes ved navigation.
4. WebView-controlleren afbryder intern navigation før browseren åbner noget og sender den til editorens validerede kommando. Samme adfærd gælder forespørgsler om et nyt vindue.
5. Opdatér hjælpetekster og tilføj tests af rendering, sikre mål, selection og ugemte ændringer. Kør samlet build/test.
