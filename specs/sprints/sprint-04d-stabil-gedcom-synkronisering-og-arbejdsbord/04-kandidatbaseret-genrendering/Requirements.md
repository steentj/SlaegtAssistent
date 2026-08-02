# Requirements: Kandidatbaseret genrendering

- Et dokument skal kende den senest anvendte skabelonidentitet.
- Et dokument skal kende den senest anvendte kanoniske GEDCOM-baseline.
- Ændret skabelon skal skabe kandidatindhold for eksisterende persondokumenter.
- Nye eller ændrede strukturerede GEDCOM-data skal skabe kandidatindhold for eksisterende persondokumenter.
- Kandidaten må kun ændre indhold mellem de genererede sektionsmarkører.
- Fri Markdown-tekst og AI-tekst uden for markørerne må ikke ændres.
- Kandidatens rækkefølge og indhold skal være deterministisk.
- Afvisning må ikke skrive til fil eller ændre en åben editor.
- Godkendelse skal markere dokumentet som ugemt, indtil brugeren gemmer.
- Dokumenter uden markører skal tilbydes migrering som separat kandidat.
