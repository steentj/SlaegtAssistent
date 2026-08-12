# Plan: Samlet regression, platform og distribution

1. Byg risikomatricen og identificér alle resterende huller uden at ændre produktkravene.
2. Skriv fejlende integrations- og rendererløse UI-tests for hvert identificeret hul.
3. Tilføj repræsentative, ikke-personfølsomme GEDCOM-testdata og genstartsscenarier.
4. Tilføj deterministiske ydelses- og responsivitetsmålinger for aftalte datastørrelser.
5. Lås SDK og NuGet-resolution reproducerbart.
6. Etablér CI-matrix for macOS, Windows og Linux.
7. Konfigurér og verificér Native AOT/enkeltfil, eller skriv en ADR med en dokumenteret blokering og indhent udtrykkelig godkendelse til en ændret distributionskontrakt.
8. Kør platformsspecifikke grundlæggende funktionstest for opstart, filvalg, preview, import, gemning og lukning.
9. Kør hele build-, test-, publicerings- og funktionsmatrixen.
10. Gennemfør samlet manuel test af en genåbnet arbejdsmappe efter sprintens `Validation.md`.
11. Dokumentér resultatet og stop. Roadmap trin 5 må først begynde efter brugerens skriftlige sprintgodkendelse.
