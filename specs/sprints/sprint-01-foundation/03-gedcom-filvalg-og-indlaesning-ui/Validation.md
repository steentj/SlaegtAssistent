# Validation: GEDCOM-filvalg og indlæsning i UI

## Definition of Done
- [ ] `dotnet test tests/SlaegtsAssistent.App.Tests` passerer med tests for: annulleret filvalg,
      succesfuld indlæsning, erstatning ved genindlæsning, og fejlvisning ved `GedcomLoadException`.
- [ ] `MainWindowViewModel` bruger kun interfaces til filvalg/notification og indeholder ingen
      direkte Avalonia-dialogtyper.
- [ ] Bruger kan vælge en `.ged`-fil fra UI, og venstre personliste opdateres med indholdet.
- [ ] Ved ugyldig/manglende fil får brugeren en tydelig fejlmeddelelse; appen fortsætter stabilt.

## How to Verify
1. Kør:
   ```
   dotnet test tests/SlaegtsAssistent.App.Tests
   ```
2. Kør appen:
   ```
   dotnet run --project src/SlaegtsAssistent.App
   ```
3. Brug "Vælg GEDCOM-fil", vælg en gyldig testfil, og bekræft at personer vises i venstre panel.
4. Gentag med en ugyldig/korrupt fil, og bekræft synlig fejlmeddelelse uden crash.

## Merge Criteria
Merge når alle DoD-punkter er markeret, testene er grønne, og reviewer kan gennemføre
end-to-end-flowet "vælg fil → indlæs data → se personer" direkte i UI.
