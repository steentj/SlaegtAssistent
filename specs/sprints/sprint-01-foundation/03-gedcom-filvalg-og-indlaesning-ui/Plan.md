# Plan: GEDCOM-filvalg og indlæsning i UI (TDD)

1. **Skriv failende test: vælg-fil kommando uden valg gør intet**
   - Test: når filvælger returnerer `null`/tom sti (brugeren annullerer), ændres `People` ikke og
     loaderen kaldes ikke.

2. **Implementer minimal `VælgGedcomFilCommand` i `MainWindowViewModel`**
   - Injicér `IFilePickerService` og `IGedcomLoader`; implementér "annuller"-flow.

3. **Skriv failende test: gyldig fil indlæser personer**
   - Test: når filvælger returnerer en sti, kaldes loaderen, og `People` fyldes med forventede
     elementer i stabil rækkefølge.

4. **Implementer mapping `FamilyTree` → `People`**
   - Opdatér `ObservableCollection<PersonListItemViewModel>` atomisk og sæt evt. første person som
     valgt standard.

5. **Skriv failende test: ny indlæsning erstatter tidligere liste**
   - Test: efter to indlæsninger med forskelligt datasæt indeholder `People` kun det nyeste
     resultat.

6. **Implementer reset/opdateringsadfærd**
   - Ryd og genopbyg listen konsistent uden dubletter fra forrige kørsel.

7. **Skriv failende test: parse-fejl vises til brugeren**
   - Test: ved `GedcomLoadException` kaldes en notifieringsservice med en brugervenlig fejltekst, og
     appen crasher ikke.

8. **Implementer fejlvisning i UI-laget**
   - Tilføj/brug `IUserNotificationService` (eller eksisterende mønster) og håndtér fejl eksplicit.

9. **Manuel smoke-test i appen**
   - Start appen, vælg en rigtig `.ged`-fil, bekræft at personlisten udfyldes.
   - Vælg en ugyldig fil og bekræft synlig fejlmeddelelse.

10. **Refactor-pass**
    - Hold ViewModel fri for Avalonia-typer.
    - Fjern dubleret mapping/logik og bevar klare interfaces.
