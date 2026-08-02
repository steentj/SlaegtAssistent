# Requirements: Sprint 04D

- Nye personer skal have status `Ny`, og GEDCOM-værdier skal være valgt som standard.
- Nye GEDCOM-oplysninger på en eksisterende person skal vises som ændringer med GEDCOM valgt som standard.
- Første import uden eksisterende Markdown-filer må ikke oprette falske ændringsdialoger for de nyoprettede dokumenter.
- Genindlæsning af samme uændrede GEDCOM-fil må ikke vise ændringer.
- En valgt GEDCOM-fil, dens importidentitet og rå personsegmenter skal gemmes lokalt.
- Rå GEDCOM-data skal kunne vises ved genåbning uden at indlæse GEDCOM-filen igen.
- Skillelinjen mellem editor/preview og kontekstpanel skal kunne flyttes.
- Editor/preview og rå GEDCOM-visning skal resizes korrekt, når panelbredden ændres.
- Skabelon- eller GEDCOM-ændringer skal skabe en kandidatopdatering af den maskingenererede sektion.
- Kandidatopdateringer må ikke ændre fri tekst eller AI-tekst automatisk.
- Afviste kandidater må ikke ændre dokumentet. Godkendte kandidater skal markere dokumentet som ugemt.
- Dokumenter uden genererede markører skal tilbydes sikker migrering uden tab af indhold.
