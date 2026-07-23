# Validation: Standardmenu og standardmapper

## Definition of Done
- [ ] Appen har menulinje med hovedmenuerne `Fil` og `Hjælp`.
- [ ] `Fil > Indlæs GEDCOM fil` åbner filvælger i standard-inputmappe, når den er sat.
- [ ] `Fil > Indstillinger` åbner separat vindue, hvor input/output-standardmapper vises og kan
      opdateres.
- [ ] Hovedbilledet har ikke separat knap til GEDCOM-indlæsning.
- [ ] Hvis standard-inputmappe ikke er sat, sættes den automatisk til mappen for den valgte
      GEDCOM-fil.
- [ ] Hvis standard-outputmappe mangler ved GEDCOM-indlæsning, bliver brugeren bedt om at vælge en
      outputmappe før indlæsning fortsætter.
- [ ] Standardmapper persisteres lokalt og genindlæses ved app-opstart.
- [ ] GEDCOM-indlæsning genererer Markdown-filer i den valgte outputmappe.
- [ ] `dotnet test tests/SlaegtsAssistent.App.Tests` passerer.

## How to Verify
1. Kør:
   ```
   dotnet test tests/SlaegtsAssistent.App.Tests
   ```
2. Start appen:
   ```
   dotnet run --project src/SlaegtsAssistent.App
   ```
3. Åbn `Fil > Indlæs GEDCOM fil` og bekræft startmappeadfærd.
4. Åbn `Fil > Indstillinger`, verificér at mappefelter vises, og opdatér begge mapper.
5. Fjern outputstandard og forsøg indlæsning; bekræft at outputmappe skal vælges først.
6. Indlæs en GEDCOM-fil og bekræft at `.md`-filer skrives i outputmappen.

## Merge Criteria
Merge når alle DoD-punkter er opfyldt, test er grønne, og menu-/indstillingsflowet kan gennemføres
ende-til-ende uden manuelle kodeændringer.
