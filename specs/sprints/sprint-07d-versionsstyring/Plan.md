# Plan: sprint 7D – versionsstyring af persondokumenter

Status: **planlagt**. Se [Analyse.md](Analyse.md) for review og beslutninger og [Implementeringsvejledning.md](Implementeringsvejledning.md) for kontrakter og præcise Git-kommandoer. Krav: [Requirements.md](Requirements.md). Validering: [Validation.md](Validation.md).

**Mål:** Brugeren kan versionere alle persondokumenter lokalt med Git gennem to knapper i hovedvinduet (Commit, Versionshistorik) og et simpelt historikvindue med Vis ændringer, Commit alle og Rul tilbage her.

**Forudsætning:** Sprint 5 og 6 er implementeret (dokumentkatalog, editorcache, `TrySaveAll`, `ActivateWorkspace`).

**Arbejdsform:** Testdrevet (røde tests først). Efter hver pakke stoppes, de angivne tests køres, og pakkens manuelle validering gennemføres, før næste pakke begynder. Fravigelser fra pakkens trin skal begrundes i Validation.md.

| Pakke | Leverance | Afhænger af |
| --- | --- | --- |
| V1 | Core-typer og parsere | – |
| V2 | Git-opslag, procesrunner-udvidelse og `GitVersionControlService` | V1 |
| V3 | `VersioningWorkflow`, Commit-knap og commitdialog i hovedvinduet | V2 |
| V4 | Versionshistorik-vindue og diffvindue | V3 |
| V5 | Tilbagerulning, regression, dokumentation og roadmap | V4 |

(Rækkefølgen sætter tilbagerulning sidst, fordi den er den eneste destruktive handling og kræver, at alt andet er afprøvet.)

---

## V1 – Core-typer og parsere

**Filer (nye):** `src/SlaegtsAssistent.Core/Versioning/` med `WorkingTreeChange.cs`, `VersionEntry.cs` (indeholder `VersionHistory`), `DiffModels.cs` (`DiffLineKind`, `DiffLine`, `DiffFile`), `GitStatusParser.cs`, `GitLogParser.cs`, `UnifiedDiffParser.cs`, `CommitMessageSuggester.cs`, `CommitIdValidator.cs`. Tests: `tests/SlaegtsAssistent.Core.Tests/Versioning/…Tests.cs`.

**Trin:**
1. Skriv fejlende tests (nedenfor). Typer og signaturer findes i Implementeringsvejledning "Core-typer".
2. Implementér typerne som `public sealed record` og parserne som `public static class` efter parserreglerne.
3. Ingen I/O, ingen `DateTime.Now` (tid sendes ind), ingen Avalonia.

**Tests (mindst):**
- `GitStatusParser`: tom input; `" M a.md\0"` ⇒ Aendret; `"?? ny.md\0"` ⇒ Ny; `" D gammel.md\0"` ⇒ Slettet; `"A  a.md\0"` ⇒ Ny; omdøbning `"R  ny.md\0gammel.md\0"` ⇒ én post (ny sti) og ingen post for den gamle; sti med mellemrum og `æøå`; resultatet sorteret.
- `GitLogParser`: to poster; tom besked; besked med `æøå`; mere end `maxEntries` ⇒ afkortet og `HasMore`; præcis `maxEntries` ⇒ `HasMore == false`; tidszone bevaret (`2026-11-12T09:23:00+01:00`).
- `UnifiedDiffParser`: ændret fil med to hunks; ny fil; slettet fil; omdøbt fil; fil med `\ No newline at end of file`; to filer i ét diff; tekstlinje der selv starter med `diff --git` (præfikset `+`) behandles som indhold; CRLF fjernes fra linjetekst; tom input ⇒ tom liste.
- `CommitMessageSuggester`: fast tidspunkt ⇒ `Ændringer 12-11-2026 09:23` (brug en `DateTimeOffset` i lokal zone, så testen ikke afhænger af maskinens zone — konstruér via `new DateTimeOffset(…, TimeZoneInfo.Local.GetUtcOffset(…))`).
- `CommitIdValidator`: gyldig 40- og 64-tegns hex; null, tom, store bogstaver, 39 tegn, `abc; rm -rf`, `--help` ⇒ false.

**Kør:** `dotnet test tests/SlaegtsAssistent.Core.Tests --filter "FullyQualifiedName~Versioning"`.
**Stop:** manuel godkendelse = testene er grønne og koden gennemlæst mod parserreglerne.

---

## V2 – Git-opslag og `GitVersionControlService`

**Filer:** nye `Services/IGitLocator.cs` (inkl. `GitLocator`), `Services/IVersionControlService.cs` (grænseflade, `VersionControlState`, `VersionStatus`, `VersionOperationResult`, `VersionControlException`), `Services/GitVersionControlService.cs`. Ændret: `Services/LocalProcessRunner.cs` (valgfrie parametre), `App.axaml.cs` (registrér `IGitLocator`, `ILocalProcessRunner` hvis den ikke allerede er registreret, `IVersionControlService`). Tests: `tests/SlaegtsAssistent.App.Tests/GitLocatorTests.cs`, `GitVersionControlServiceTests.cs`, `GitFactAttribute.cs`, udvidelse af `LocalProcessRunnerTests.cs`.

**Trin:**
1. Udvid `LocalProcessRequest`/`LocalProcessRunner` efter vejledningen. Kør eksisterende `LocalProcessRunnerTests` og `GraphvizLayoutServiceTests` – de skal være uændrede og grønne.
2. Skriv `GitFactAttribute` og testhjælper `TempWorkspace` (opretter mappe, rydder op, sætter isoleret HOME/`GIT_CONFIG_GLOBAL`).
3. Skriv fejlende integrationstests, derefter `GitLocator` og `GitVersionControlService` metode for metode i denne rækkefølge: `GetStatusAsync` → `EnableAsync` → `GetHistoryAsync` → `CommitAllAsync` → `GetWorkingChangesAsync` → `GetCommitChangesAsync` → `RollbackToAsync` (rollback implementeres her, men får først brugerflade i V5).
4. Servicen bruger `Core`-parserne til al fortolkning. Den må ikke indeholde UI-tekster ud over danske fejlbeskeder.

**Tests (alle `GitFact` med rigtige midlertidige arkiver):**
- Status: mappe uden arkiv ⇒ `IkkeAktiveret`; arkiv i overliggende mappe ⇒ `ForkertArkiv` og **intet** skrives i det overliggende arkiv; `GitLocator` uden Git (fake-locator der giver `null`) ⇒ `GitMangler` (kan køres uden Git).
- Aktivér: opretter `.git`, `.gitignore`, én start-commit med eksisterende `.md`-filer; arkivlokal identitet sat; brugerens globale konfiguration ikke ændret; kald to gange er sikkert (idempotent, ingen ekstra commit hvis intet ændret, eller en tom start-commit højst én gang).
- `.gitignore`: `.slaegtsassistent/manifest.json`, undermapper og `.a.md.<guid>.tmp` ignoreres; `a.md` ignoreres ikke (`git check-ignore`).
- Status efter ændring, ny fil, sletning ⇒ korrekte arter og antal; `.slaegtsassistent/**` og ikke-`.md`-filer tælles ikke; `LastChangeTime` = højeste ændringstidspunkt blandt ikke-slettede; kun sletninger ⇒ `null`.
- Historik: nyeste først; `æøå` i besked og filnavn; `HasMore` ved over grænsen.
- Commit alle: committer ændrede, nye og slettede dokumenter; blank besked afvises; uden ændringer ⇒ `Success == false` med besked og ingen tom commit; commit virker uden global Git-identitet. Staged ændringer uden for dokumentomfanget afvises uden commit eller indeksændring.
- Hooks: opsæt lokale og globale hooks for `pre-commit`, `prepare-commit-msg`, `commit-msg` og `post-commit`, som skriver markørfiler; aktivering og alle commits lykkes uden markører.
- Attributter: `*.md text eol=lf` og konverteringsfilter afvises før aktivering/commit/rollback; CRLF-dokumenter round-trippes byte-for-byte uden aktive konverteringsattributter.
- Vis ændringer (ikke committet): ny fil vises som tilføjet; status-antal er uændret bagefter.
- Vis ændringer (commit): inkl. rodcommit (start-commit viser alle filer som nye).
- Diff over 1 MB (fx 3 000 filer eller en stor fil) ⇒ henter op til 8 mio. tegn; over grænsen ⇒ `VersionControlException` med dansk tekst.
- Tilbagerulning: (a) slettet fil gendannes; (b) fil oprettet efter målet fjernes; (c) ændret indhold gendannes byte-for-byte inkl. CRLF; (d) ikke committede ændringer committes først som "Automatisk version før tilbagerulning"; (e) ny commit "Rul tilbage til …: <besked>" oprettes og historikken er intakt; (f) ugyldigt id (`--help`, `abc`) afvises uden at køre Git; (g) aktuelle bytes og filnavne lig målet, også med en anden HEAD og nye/slettede filer, giver "allerede i denne tilstand" uden ny commit; (h) `.slaegtsassistent/**` er uberørt; (i) commitfejl, timeout og annullering genopretter og verificerer snapshot af seneste commit; (j) staged filer uden for dokumentomfanget afvises uden ændring.
- Samtidige kald serialiseres (to samtidige `CommitAllAsync` giver ikke `index.lock`-fejl).

**Kør:** `dotnet test tests/SlaegtsAssistent.App.Tests --filter "FullyQualifiedName~Git|FullyQualifiedName~LocalProcessRunner|FullyQualifiedName~Graphviz"`.
**Stop:** manuel godkendelse = alle tests grønne, og `git log` i en testmappe er læsbar i et almindeligt Git-værktøj (fx `git log --stat` viser korrekt UTF-8).

---

## V3 – Commit-knap, workflow og dialoger

**Filer:** nye `Services/IVersionDialogService.cs`, `Services/AvaloniaVersionDialogService.cs`, `Services/IVersioningHost.cs`, `Services/VersioningWorkflow.cs`, null-implementeringer i `MainWindowViewModel` (privat `NullVersionDialogService`, `NullVersionControlService` – følg eksisterende `Null…`-mønster). Ændret: `ViewModels/MainWindowViewModel.cs` (implementér `IVersioningHost`, nye valgfrie konstruktørparametre `IVersionControlService? versionControlService = null`, `VersioningWorkflow? versioningWorkflow = null`, `IVersionHistoryWindowService? versionHistoryWindowService = null`; kommandoerne `CommitCommand`, `ShowVersionHistoryCommand`), `Views/MainWindow.axaml` (to knapper efter **Gem**), `App.axaml.cs`. Tests: `tests/SlaegtsAssistent.App.Tests/VersioningWorkflowTests.cs`, `MainWindowViewModelVersioningTests.cs`.

**Trin:**
1. Skriv fejlende tests for `VersioningWorkflow.CommitAllAsync` med fakes (rækkefølge og alle grene i "Flow → Commit").
2. Implementér `VersioningWorkflow`. Ingen Avalonia-typer.
3. `MainWindowViewModel`: `IVersioningHost`-medlemmer: `WorkspaceFolder => StandardMarkdownOutputFolder`, `IsBusyWithImport => IsImporting`, `HasUnsavedEditors => HasDirtyEditors`, `SaveAllEditors() => TrySaveAll()`, `ReloadWorkspace() => ActivateWorkspace()`, event `DocumentsSaved` rejses i `TrySaveAll` efter succes. `CommitCommand` (async, `CanExecute = !IsImporting`) kalder `_versioningWorkflow.CommitAllAsync(this)`; `ShowVersionHistoryCommand` kalder `_versionHistoryWindowService.Show(this)`. Opdatér `CanExecute` ved ændring af `IsImporting` (som `SaveAllCommand`).
4. `AvaloniaVersionDialogService`: modal `Window` efter mønstret i `AvaloniaUnsavedChangesDialogService`. `AskCommitMessageAsync`: titel "Commit", kort tekst "Beskriv kort, hvad der er ændret:", `TextBox` forudfyldt med forslaget og markeret, knapper **Annullér** og **Commit** (Commit deaktiveret ved blank tekst; Enter = Commit; Esc = Annullér), tilgængelige navne (`AutomationProperties.Name`). `ConfirmAsync`: titel, brødtekst med `TextWrapping`, knapper **Annullér** og `confirmText`.
5. `MainWindow.axaml`: tilføj `<Button Content="Commit" Command="{Binding CommitCommand}" ToolTip.Tip="Gem og commit alle ændringer i alle dokumenter som en ny version." />` og `<Button Content="Versionshistorik" Command="{Binding ShowVersionHistoryCommand}" />` i værktøjslinjen efter "Gem". Brug eksisterende stilarter.
6. Registrér `IVersionDialogService`, `VersioningWorkflow` (singleton) og de øvrige i `App.axaml.cs`. Grænsefladerne `IVersionHistoryWindowService` og `IVersionDiffWindowService` oprettes allerede i V3 (workflowet afhænger af den sidste) sammen med `NullVersionHistoryWindowService` og `NullVersionDiffWindowService` (ingen handling). De Avalonia-baserede implementeringer og registreringerne tilføjes i V4.

**Tests (mindst):**
- Workflow: `IkkeAktiveret` + bekræftet ⇒ `SaveAllEditors` kaldes før `EnableAsync`; afvist ⇒ intet kaldes; gemmefejl ⇒ ingen aktivering/startcommit; `GitMangler` ⇒ besked og ingen commit; ugemte editorer i aktiveret arkiv gemmes før status/commit; mislykket gemning ⇒ ingen commit; 0 ændringer ⇒ besked og ingen dialog; annulleret/blank besked ⇒ ingen commit; succes ⇒ `CommitAllAsync` kaldt med brugerens besked; fejl fra servicen vises; import i gang ⇒ afbrudt; ingen arbejdsmappe ⇒ besked "Vælg først en arbejdsmappe i Indstillinger.".
- Main-VM: `CommitCommand` er deaktiveret under import; standardkonstruktøren virker uændret (alle eksisterende tests grønne); `DocumentsSaved` rejses efter `SaveAll`.
- Headless: `MainWindow` åbner og viser knapperne "Commit" og "Versionshistorik".

**Kør:** `dotnet test tests/SlaegtsAssistent.App.Tests --filter "FullyQualifiedName~Version"` og derefter hele `dotnet test`.
**Stop:** manuel validering V3 i Validation.md (tryk Commit i en rigtig arbejdsmappe).

---

## V4 – Versionshistorik-vindue og diffvindue

**Filer:** nye `ViewModels/VersionHistoryViewModel.cs` (inkl. `VersionEntryViewModel`), `ViewModels/VersionDiffViewModel.cs`, `Views/VersionHistoryWindow.axaml(.cs)`, `Views/VersionDiffWindow.axaml(.cs)`, `Services/IVersionHistoryWindowService.cs` + `AvaloniaVersionHistoryWindowService.cs`, `Services/IVersionDiffWindowService.cs` + `AvaloniaVersionDiffWindowService.cs`. Ændret: `App.axaml.cs`. Tests: `VersionHistoryViewModelTests.cs`, `VersionDiffViewModelTests.cs`, `VersionWindowsHeadlessTests.cs`.

**`VersionHistoryViewModel`** (`: ViewModelBase`, afhænger af `IVersioningHost`, `IVersionControlService`, `VersioningWorkflow`):
- Egenskaber: `StatusText`, `ExtraStatusText` (ugemte editorer), `HasChanges`, `ShowEnableButton`, `Entries` (`ObservableCollection<VersionEntryViewModel>`), `HistoryNote` ("Viser de seneste 500 versioner." når `HasMore`), `InfoMessage`, `ErrorMessage`, `IsBusy`.
- Kommandoer: `RefreshCommand`, `ShowChangesCommand` (ikke committede), `CommitAllCommand`, `EnableCommand`. Alle bruger `CanExecute` efter tabellen "Tilstande og knapper".
- `RefreshAsync` henter status og historik (500) og fylder listen; nyeste øverst (Git-rækkefølge). Kaldes ved åbning, efter hver handling, ved `host.DocumentsSaved` og når vinduet aktiveres (`Activated` i code-behind kalder `RefreshCommand`).
- Kun én opdatering ad gangen; seneste kald vinder (annullér forrige `CancellationTokenSource`).
- `VersionEntryViewModel`: `Id`, `DateText` (`dd-MM-yyyy`), `TimeText` (`HH:mm`, lokal tid), `Message`, `ShowChangesCommand`, `RollbackCommand` (tilbagerulning forbindes i V5; i V4 er den deaktiveret og skjult via `IsRollbackVisible = false`). Kommandoerne oprettes med `Func<…>`-callbacks fra forældren – ingen reference til Avalonia.

**Vindue `VersionHistoryWindow.axaml`:** titel "Versionshistorik"; øverst en `Border` med `StatusText`, `ExtraStatusText` og de to knapper **Vis ændringer** og **Commit alle** (rækkefølge og tekst præcist som ønsket) samt en mindre **Opdatér**; ved `ShowEnableButton` vises **Slå versionsstyring til**; under en tydelig fejl-/infolinje; derefter en `ListBox` med `ItemTemplate`: `Grid` med kolonner dato | tid | besked (tekstafkortning med tooltip) | knapper **Vis ændringer** og **Rul tilbage her**. Kolonneoverskrifter "Dato", "Tid", "Besked" over listen. Brug `DynamicResource`-farver fra eksisterende designsystem (se `Views/MainWindow.axaml`); ingen hårdkodede farver. Tilgængelighed: `AutomationProperties.Name` på knapper og liste; tastaturnavigation (Tab, Enter).

**Vindue `VersionDiffWindow.axaml`:** to kolonner med `GridSplitter`: venstre `ListBox` med filer (sti + art "Ny"/"Slettet"/"Ændret"); højre `ScrollViewer` med `ItemsControl` over linjer i fast skrift. Linjer farves efter `DiffLineKind` via en `IValueConverter`/stilklasser, der giver Added = grønlig, Removed = rødlig, HunkHeader = dæmpet/blå med semitransparente baggrunde, der virker i både lyst og mørkt tema. Titel fra `Show(title, …)`. Notitslinje øverst ved afkortning. `VersionDiffViewModel` afkorter til 20 000 linjer samlet og sætter notits. Ingen redigering; teksten kan markeres og kopieres.

**Vinduesservices:** efter `AvaloniaGedcomHelpService`-mønstret (ét vindue genbruges; `Activate()` ved genåbning; `window.Show(owner)`; nulstil ved `Closed`). Historikservicen opretter `VersionHistoryViewModel` med den medsendte `host`. Diffservicen erstatter indholdet i det åbne vindue (`DataContext`) i stedet for at åbne flere. Registrér begge i `App.axaml.cs`, og send historikservicen til `MainWindowViewModel`.

**Tests (mindst):**
- Statuslinjetekst for alle tilstande og for 0, 1 og flere ændringer; `LastChangeTime == null` ⇒ "Seneste ændring: tidspunkt ukendt."; ekstra linje ved ugemte editorer.
- Knapstater (også under `IsBusy` og import); "Vis ændringer"/"Commit alle" deaktiveret ved 0 ændringer; ingen knapper aktive ved `GitMangler`/`ForkertArkiv`; kun "Slå til" ved `IkkeAktiveret`.
- Listen er nyeste øverst; dato/tid-formatering; note ved `HasMore`.
- Commit alle kalder workflowet og opdaterer derefter listen og status; Vis ændringer kalder diffservicen med `Show(…)` og viser fejl fra `VersionControlException`.
- Hurtige gentagne `RefreshCommand` giver ét endeligt, konsistent resultat.
- `VersionDiffViewModel`: afkortning ved 20 000 linjer; valg af fil viser dens linjer.
- Headless: begge vinduer åbner med fakes, viser de to knapper og en liste, og lukker uden fejl; gentagen `Show` skaber ikke dubletvinduer.

**Kør:** `dotnet test tests/SlaegtsAssistent.App.Tests --filter "FullyQualifiedName~Version"` og derefter hele `dotnet test`.
**Stop:** manuel validering V4.

---

## V5 – Tilbagerulning, regression, dokumentation, roadmap

**Filer:** ændret `ViewModels/VersionHistoryViewModel.cs` (aktivér `RollbackCommand`), `Services/VersioningWorkflow.cs` (`RollbackAsync`), tests, `specs/Roadmap.md`, `CHANGELOG.md` (brug skill `changelog-by-date`), `README.md` (kort afsnit), evt. hjælpetekst.

**Trin:**
1. Fejlende tests for `VersioningWorkflow.RollbackAsync`; implementér efter "Flow → Rul tilbage her". Rækkefølge: bekræftelse ⇒ erhverv fælles arbejdsområdespærring ⇒ `SaveAllEditors` ⇒ `RollbackToAsync` inkl. verifikation/genopretning ⇒ `ReloadWorkspace` ⇒ frigiv spærring ⇒ resultat.
2. Aktivér `RollbackCommand` i listen (`IsRollbackVisible = true`); deaktiver for den nyeste version uden ændringer.
3. Efter tilbagerulning opdateres historik/status, og `InfoMessage` viser "Dokumenterne er rullet tilbage til <dato tid>." eller den "allerede i denne tilstand"-besked.
4. **Regressionstest:** kør hele `dotnet test`. Udvid `AvaloniaHeadlessRegressionTests` med en test af at hovedvinduets øvrige knapper er uændrede.
5. **Roadmap:** "Trin 13: Versionsstyring … SPRINT 7D" er allerede indsat i `specs/Roadmap.md` (AI, RAG og HTR er omnummereret til Trin 14–16). Kontrollér blot, at status og links stadig passer, og opdatér status til "gennemført" når sprintet er godkendt.
6. `README.md`: kort afsnit "Versionsstyring" (kræver Git; hvad der versioneres; tilbagerulning er ikke-destruktiv).
7. Opdatér `CHANGELOG.md` efter skill `changelog-by-date`.
8. Udfyld "Registrering" i Validation.md med de faktiske resultater (også afvigelser og ikke-kørte tests).

**Tests (mindst):**
- Workflow: afvist bekræftelse ⇒ intet sker; fælles spærring blokerer redigering, gemning, import, mappeskift og skabelonanvendelse under forsinket rollback; ugemte editorer gemmes først; mislykket gemning ⇒ ingen tilbagerulning; succes/genoprettet fejltilstand ⇒ `ReloadWorkspace` kaldes **før** spærringen frigives; fejl vises uden at love verificeret genopretning, hvis den fejler; "allerede i denne tilstand" ⇒ ingen commit eller genindlæsning.
- Integration (`GitFact`): ende-til-ende med rigtig `MainWindowViewModel`, rigtig service og rigtig midlertidig arbejdsmappe: gem et dokument, Commit, ændr, Commit, rul tilbage til første version, kontrollér filens indhold, editorens tekst efter genindlæsning og historikkens fire poster (to brugerversioner, start-commit, tilbagerulning).

**Kør:** `dotnet build` og hele `dotnet test`.
**Stop:** samlet manuel validering af hele sprintet (Validation.md) og skriftlig godkendelse.

---

## Udenfor sprintet (opfølgning)

Automatisk commit før GEDCOM-import og skabelonanvendelse; medpakning af privat Git pr. platform (kun pakning – opslaget findes); valgfri remote-backup efter privatlivsvurdering; sammenligning af to vilkårlige versioner; gendannelse af enkelte dokumenter.
