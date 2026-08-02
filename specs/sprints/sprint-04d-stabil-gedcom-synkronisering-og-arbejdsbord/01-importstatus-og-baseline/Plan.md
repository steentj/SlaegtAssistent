# Plan: Importstatus og synkroniseringsbaseline

1. Skriv failing Core- og App-tests for ny person, ny oplysning, uændret genimport og første import uden dokumenter.
2. Definér statusmodellen `Ny`, `Uændret`, `Ændret` og eventuelle fejltilstande.
3. Tilføj en kanonisk, deterministisk baseline for de importerede strukturerede GEDCOM-data.
4. Adskil oprettelse af nye dokumenter fra sammenligning af eksisterende dokumenter.
5. Giv nye dokumenter GEDCOM som standardvalg og undlad ændringsdialog for dokumenter, der blev oprettet i samme import.
6. Gør genimport af samme baseline til et no-op.
7. Stop for manuel validering med første import, gentagen import og en import med én ændret oplysning.
