# Plan: Kandidatbaseret genrendering

1. Skriv failing Core-tests for skabelonhash, GEDCOM-baseline, genereret sektion, godkendelse, afvisning og fri tekst.
2. Udvid dokumentmetadata med versionsstyret hash eller identitet for skabelon og GEDCOM-baseline.
3. Implementér en deterministisk reconciler, der genererer en kandidat ved ændret skabelon eller GEDCOM-data.
4. Begræns kandidatændringen til markerede genererede sektioner.
5. Bevar fri Markdown-tekst og AI-tekst byte-for-byte uden for den genererede sektion.
6. Vis nye GEDCOM-data og skabelonændringer som en gennemgåelig diff med GEDCOM/skabelon valgt som standard for nye felter.
7. Lad brugeren godkende eller afvise kandidaten. Godkendelse ændrer editoren til ugemt; afvisning ændrer intet.
8. Tilbyd sikker migrering af dokumenter uden markører og skriv aldrig automatisk over et sådant dokument.
9. Stop for manuel validering med ændret template, nye GEDCOM-felter, afvist kandidat og godkendt kandidat.
