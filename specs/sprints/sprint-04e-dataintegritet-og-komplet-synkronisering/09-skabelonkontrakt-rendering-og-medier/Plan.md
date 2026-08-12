# Plan: Valideret skabelonkontrakt, rendering og medier

1. Skriv fejlende tests for ukendte felter, forkert løkkekontekst og alle offentlige felter.
2. Skriv fejlende referenceoutputtests for to forældre, danske kategorier og deterministisk output.
3. Skriv fejlende tests for relative, absolutte, manglende og utilgængelige mediefiler.
4. Definér et versionsstyret feltskema og validér hele skabelonens AST.
5. Genbrug valideringen ved indstillingsgemning, preview og importpreflight.
6. Ret standardskabelon og dansk præsentation uden at ændre brugerens fritekst.
7. Implementér en lokal medieopløser med tydelig diagnostik og sikker stipolitik.
8. Opdatér skabelon-cheat sheetet fra eller mod samme offentlige kontrakt.
9. Kør målrettede tests, hele testpakken og build.
10. Gennemfør manuel skabelon- og medietest fra separate GEDCOM- og outputmapper.
11. Dokumentér resultatet, og stop før feature 10.
