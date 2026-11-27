# Validering af billeder i editoren

- Indsæt et eksisterende lokalt billede via almindelig Markdown og via billedvælgeren. Kontrollér rendering efter markørflytning, klik til rå syntaks, fortryd og genåbning.
- Prøv relative stier, mellemrum, breddeangivelser, referencer, manglende filer og eksterne URL'er. Kontrollér, at det sikre lokale opslag deles med forhåndsvisningen.
- Skift dokument og slå rå visning til og fra. Kontrollér, at billederne følger det aktuelle dokument, og at skift mellem visninger ikke ændrer den gemte Markdown.

## Resultat

Den 27. november 2026 bestod de automatiske Avalonia-prøver af almindelig billedsyntaks og referencebilleder med en relativ sti med æ og mellemrum, bredde på 25 procent, markørflytning, klik tilbage til syntaks og rå visning. De eksisterende arbejdsbordsprøver dækkede billedindsættelse, topbilleder og fortrydelse. Den fælles billedkontrol bestod prøver med netværksstier, filer uden for tilladte mapper og symbolske links.
