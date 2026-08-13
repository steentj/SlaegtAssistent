# Requirements: Feltstyret konfliktløsning og kandidatgodkendelse

- Strukturerede forskelle skal vises felt for felt med stabil rækkefølge, feltsti, dokumentværdi, senest godkendt værdi og ny GEDCOM-værdi.
- Gentagne data som hændelser, kilder og medier skal have stabile elementidentiteter og handlingerne tilføj, ændr og fjern.
- Nye GEDCOM-værdier må være valgt som standard, men må ikke skrives uden brugerens samlede godkendelse.
- Brugeren skal kunne vælge individuelt samt anvende tydelige massevalg.
- UI skal angive, om kandidaten skyldes GEDCOM, skabelon, baseline-migrering eller flere årsager.
- Valgte feltbeslutninger skal danne en previewkandidat for den genererede sektion før commit.
- Kandidaten må kun ændre metadata og indhold mellem de genererede markører.
- Fri Markdown- og AI-tekst uden for markørerne skal bevares byte-for-byte.
- Afvisning og lukning uden anvendelse må ikke ændre fil, editor, baseline eller snapshot.
- Godkendelse skal markere en åben editor som ugemt; lukket dokument må først skrives i importens atomiske commit.
- Dokumenter uden markører og dokumenter med manglende baseline skal have særskilt, tydelig migreringshandling.

Den normative beslutningsmodel, feltstierne og commitgrænsen er dokumenteret i [Konfliktmodel.md](Konfliktmodel.md).
