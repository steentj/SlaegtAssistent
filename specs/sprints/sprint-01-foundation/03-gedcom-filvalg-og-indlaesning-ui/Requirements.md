# Feature: GEDCOM-filvalg og indlæsning i UI

## Scope
Tilføj den manglende UI-funktionalitet til at vælge en `.ged`-fil fra disk og indlæse den via
applikationens GEDCOM-loader, så personlisten i venstre panel bliver udfyldt med data. Denne
feature dækker Trin 1-opgaven om at gøre GEDCOM-indlæsning tilgængelig fra brugerfladen.

## In Scope
- En synlig handling i UI (fx knap/menu) til at åbne en filvælger for `.ged`-filer.
- ViewModel-flow der kalder `IGedcomLoader` med den valgte filsti.
- Mapping fra indlæst `FamilyTree` til personlisten (`PersonListItemViewModel`) i venstre panel.
- Fejlvisning til brugeren ved ugyldig fil, manglende fil, eller parse-fejl (ingen tavse fejl).
- Nulstilling/opdatering af tidligere indlæste personer ved ny filindlæsning.

## Out of Scope
- Selve GEDCOM-parse-logikken og domænemappingen i `Core` (feature 02).
- Generering af Markdown-filer (feature 04).
- Editor/Preview-indhold for en valgt person (feature 05).
- Avanceret MRU/filhistorik eller drag-and-drop.

## Decisions & Context
- **Separation af ansvar:** Filvælgeren abstraheres bag et interface (fx `IFilePickerService`), så
  `MainWindowViewModel` kan testes uden native dialoger.
- **Fejlhåndtering:** `GedcomLoadException` oversættes til en brugervenlig tekst i UI-laget via en
  notifierings-/dialogservice. Fejl må ikke sluges.
- **Dataflow:** `Vælg fil` → `IFilePickerService` returnerer sti → `IGedcomLoader.Load(sti)` →
  `MainWindowViewModel.People` opdateres atomisk.
- **Testbarhed:** ViewModels må fortsat være fri for Avalonia UI-typer; UI-specifikke dialogklasser
  holdes i App-laget bag interfaces.

## Dependencies
- Kræver feature 01 (UI-shell og `MainWindowViewModel`).
- Kræver feature 02 (`IGedcomLoader`, `FamilyTree`, `Person` og `GedcomLoadException`).
- Feature 04 og 05 forventer, at denne feature kan levere valgt person/indlæst datasæt i UI.
