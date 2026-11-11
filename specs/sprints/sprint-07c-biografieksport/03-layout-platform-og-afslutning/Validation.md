# Validering: Layout, platformspakker og afslutning

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Kommandoer: dotnet restore; dotnet build; dotnet test; Python unittest discover fra 7B. Kør hver kommando særskilt og registrér resultat.
- Platformmatrix: OS, arkitektur, .NET/Avalonia, Graphviz, Python/Pandoc/WeasyPrint, font og kontorprogramversion.
- Visuel evidens: syntetiske eksempeloutputs, PDF-renderinger og noter om faktiske kontorprogrammer/systemprint.
- Slutaccept: gennemgå alle tre sprinters Validation.md og markér afvigelser som åbne, indtil de er rettet og afprøvet.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | B-09, B-10, B-11 samt samlet G-/X-/B-krav | Ikke registreret | Afventer implementering | Ingen accept endnu |
