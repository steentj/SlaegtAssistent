# Plan: Standardmenu og standardmapper (TDD)

1. **Skriv failende tests for indstillinger i ViewModel**
   - Test: standard-inputmappe sendes videre til GEDCOM-filvælger.
   - Test: manglende inputstandard sættes fra valgt GEDCOM-fils mappe.
   - Test: manglende outputstandard kræver mappevalg før loader kaldes.

2. **Udvid service-abstraktioner i App-laget**
   - Tilføj mappevælger-interface, indstillingsservice og dialogservice til indstillingsvindue.
   - Tilføj informationsdialog-service til Hjælp-menuens handlinger.

3. **Implementer settings-flow i `MainWindowViewModel`**
   - Indlæs gemte standardmapper ved opstart.
   - Gem opdaterede mapper ved brugerhandling eller implicit første GEDCOM-valg.
   - Blokér GEDCOM-indlæsning ved manglende outputmappe.

4. **Integrer Markdown-outputskrivning ved GEDCOM-indlæsning**
   - Kald eksportservice med indlæst `FamilyTree` og valgt outputmappe.

5. **Implementer Avalonia-services**
   - Filvælger med foreslået startmappe.
   - Mappevælger til output/input-indstillinger.
   - Lokal JSON-baseret persistens for standardmapper.

6. **Tilføj standard menulinje i `MainWindow.axaml`**
   - Fil + Hjælp med de aftalte menupunkter og kommando-bindinger.
   - Fjern separat indlæsningsknap fra hovedbilledet.
   - Åbn separat indstillingsvindue fra `Fil > Indstillinger`.

7. **Kør målrettede tests**
   - `dotnet test tests/SlaegtsAssistent.App.Tests`
