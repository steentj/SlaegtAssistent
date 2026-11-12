# Implementeringsvejledning: versionsstyring (sprint 7D)

Startpunkt for en kodningsmodel med begrænset kontekstvindue. Status: **planlagt**; ingen produktionskode er skrevet.

## Læserækkefølge

1. `AGENTS.md` og `specs/Mission.md`.
2. [Analyse.md](Analyse.md) (beslutninger – følg dem, tag dem ikke op igen).
3. Dette dokument (kontrakter og præcise Git-kommandoer).
4. [Plan.md](Plan.md) for den aktuelle arbejdspakke, samt [Requirements.md](Requirements.md) og [Validation.md](Validation.md).

Arbejdspakkerne **V1–V5** udføres i rækkefølge. Efter hver pakke: kør de nævnte tests, og **stop for manuel godkendelse**, før næste pakke påbegyndes. Opgaveformulering til modellen: *"Implementér kun pakke Vn i specs/sprints/sprint-07d-versionsstyring/Plan.md. Følg Implementeringsvejledning.md. Skriv testene først. Genbrug eksisterende services og mønstre. Skriv alle tekster og kommentarer på dansk. Kør de angivne tests og rapportér resultatet."*

## Generelle regler

- Dansk i al UI-tekst, kommentarer og dokumentation; engelske identifikatorer.
- **Ingen nye NuGet-pakker.** Ingen ændring i `packages.lock.json`/`packages.distribution.lock.json`.
- Core (`src/SlaegtsAssistent.Core/Versioning/`) indeholder kun rene typer og parsere uden I/O og uden Avalonia. App indeholder proces, filsystem, UI og ViewModels. ViewModels bruger ingen Avalonia-typer.
- Tests bruger xUnit og FluentAssertions. Core-tests i `tests/SlaegtsAssistent.Core.Tests`, App-tests i `tests/SlaegtsAssistent.App.Tests`.
- Nye services registreres i `App.axaml.cs` (`ConfigureServices`). `MainWindowViewModel` får nye **valgfrie** konstruktørparametre sidst i listen (som `gedcomHelpService`) med `Null…`-standarder, så eksisterende tests og standardkonstruktøren virker uændret.
- Følg mønsteret fra `AvaloniaGedcomHelpService` (ét genbrugt, ikke-modalt vindue, `window.Show(owner)`, `Activate()` hvis åbent).
- Datoformat i UI: `dd-MM-yyyy` og `HH:mm` med `CultureInfo.InvariantCulture` (ingen afhængighed af kulturdata i Native AOT). Tider vises i lokal tid.

## Core-typer (`namespace SlaegtsAssistent.Core.Versioning`)

```csharp
public enum WorkingTreeChangeKind { Aendret, Ny, Slettet }
public sealed record WorkingTreeChange(string Path, WorkingTreeChangeKind Kind);

public sealed record VersionEntry(string Id, DateTimeOffset Time, string Message);
public sealed record VersionHistory(IReadOnlyList<VersionEntry> Entries, bool HasMore);

public enum DiffLineKind { Context, Added, Removed, HunkHeader }
public sealed record DiffLine(DiffLineKind Kind, string Text);
public sealed record DiffFile(string Path, WorkingTreeChangeKind Kind, IReadOnlyList<DiffLine> Lines);

public static class GitStatusParser   { public static IReadOnlyList<WorkingTreeChange> Parse(string porcelainZ); }
public static class GitLogParser      { public static VersionHistory Parse(string output, int maxEntries); }
public static class UnifiedDiffParser { public static IReadOnlyList<DiffFile> Parse(string diffText); }
public static class CommitMessageSuggester { public static string Suggest(DateTimeOffset now); } // "Ændringer 12-11-2026 09:23"
public static class CommitIdValidator { public static bool IsValid(string? id); }               // ^[0-9a-f]{40,64}$
```

Parserregler:

- **GitStatusParser** (`git status --porcelain=v1 -z`): poster adskilles af `\0`; hver post er `XY<mellemrum>sti`. `??` og `A` ⇒ `Ny`; `D` (i X eller Y) ⇒ `Slettet`; alt andet ⇒ `Aendret`. Er X eller Y `R` eller `C`, følger en ekstra `\0`-post med oprindelig sti, som springes over. Tom input ⇒ tom liste. Sortér efter sti (ordinal, ikke versalfølsom sortering undgås: brug `StringComparer.OrdinalIgnoreCase`).
- **GitLogParser**: format `%H%x1f%cI%x1f%s%x1e`. Del på `\x1e`; trim indledende `\n`/`\r`; ignorér tomme poster; del hver post i præcis 3 felter på `\x1f` (besked kan være tom). Tid parses med `DateTimeOffset.Parse(…, CultureInfo.InvariantCulture)`. Hvis der er flere end `maxEntries` poster, returneres de første `maxEntries` og `HasMore = true`. Rækkefølgen bevares (Git leverer nyeste først).
- **UnifiedDiffParser**: afsnit starter ved linjer der begynder med `diff --git `. Sti: tag fra `+++ b/<sti>`; er den `+++ /dev/null`, tag fra `--- a/<sti>`; ved `rename to <sti>` bruges den nye sti. Art: `new file mode` ⇒ `Ny`; `deleted file mode` ⇒ `Slettet`; ellers `Aendret`. Alt før første `@@` i afsnittet ignoreres. Derefter: `@@…` ⇒ `HunkHeader`; `+…` ⇒ `Added`; `-…` ⇒ `Removed`; mellemrum-præfiks ⇒ `Context`; `\ No newline at end of file` ⇒ springes over. Tekst er linjen **uden** det første tegn (undtagen `HunkHeader`, som beholder hele linjen) og uden afsluttende `\r`. Del tekst på `\n`.
- **CommitMessageSuggester**: `$"Ændringer {now.ToLocalTime():dd-MM-yyyy HH:mm}"` med `InvariantCulture`.

## App-kontrakter (`namespace SlaegtsAssistent.App.Services`)

```csharp
public enum VersionControlState { GitMangler, IkkeAktiveret, ForkertArkiv, Klar }

public sealed record VersionStatus(
    VersionControlState State,
    string? Message,                       // dansk forklaring ved ikke-Klar
    IReadOnlyList<WorkingTreeChange> Changes,
    DateTimeOffset? LastChangeTime)        // null hvis ingen ændringer eller kun sletninger
{ public int ChangedFileCount => Changes.Count; }

public sealed record VersionOperationResult(bool Success, string? Message);   // Message = dansk fejl/info

public interface IGitLocator { string? FindGitExecutable(); }                  // fuld sti eller null

public interface IVersionControlService
{
    Task<VersionStatus> GetStatusAsync(string workspace, CancellationToken ct);
    Task<VersionOperationResult> EnableAsync(string workspace, CancellationToken ct);
    Task<VersionHistory> GetHistoryAsync(string workspace, int maxEntries, CancellationToken ct);
    Task<VersionOperationResult> CommitAllAsync(string workspace, string message, CancellationToken ct);
    Task<IReadOnlyList<DiffFile>> GetWorkingChangesAsync(string workspace, CancellationToken ct);
    Task<IReadOnlyList<DiffFile>> GetCommitChangesAsync(string workspace, string commitId, CancellationToken ct);
    Task<VersionOperationResult> RollbackToAsync(string workspace, string commitId, CancellationToken ct);
}

// Bruges af historikvinduet; implementeres af MainWindowViewModel.
public interface IVersioningHost
{
    string? WorkspaceFolder { get; }
    bool IsBusyWithImport { get; }
    bool HasUnsavedEditors { get; }
    bool SaveAllEditors();                         // wrapper om TrySaveAll(); false ved fejl
    void ReloadWorkspace();                        // wrapper om ActivateWorkspace()
    event EventHandler? DocumentsSaved;            // rejses efter vellykket gemning; historikvinduet opdaterer status
}

public interface IVersionDialogService
{
    Task<string?> AskCommitMessageAsync(string suggestedMessage);                   // null = annulleret
    Task<bool> ConfirmAsync(string title, string message, string confirmText);
}
public interface IVersionHistoryWindowService { void Show(IVersioningHost host); }
public interface IVersionDiffWindowService     { void Show(string title, IReadOnlyList<DiffFile> files, string? notice); }
```

`GetWorkingChangesAsync`/`GetCommitChangesAsync` kaster `VersionControlException` (dansk `Message`) ved fejl, herunder `Diff for stort`. `VersionControlException : Exception` oprettes i App. De øvrige metoder kaster ikke for forventede fejl; de returnerer `VersionOperationResult(false, besked)` eller `VersionStatus` med tilstand.

## Udvidelse af `LocalProcessRunner` (bagudkompatibel)

`LocalProcessRequest` er en positional record i `Services/LocalProcessRunner.cs`. Tilføj **valgfrie** parametre til sidst, så eksisterende kald (Graphviz) ikke ændres:

```csharp
IReadOnlyDictionary<string,string>? Environment = null,
bool Utf8Output = false,
int MaximumOutputCharacters = 1_048_576
```

I `RunAsync`: kopiér `Environment` til `start.Environment`; hvis `Utf8Output`: `start.StandardOutputEncoding = start.StandardErrorEncoding = new UTF8Encoding(false)`; send `MaximumOutputCharacters` videre til `ReadBoundedAsync` for stdout (stderr beholder 4 MB). Tilføj en test i `LocalProcessRunnerTests` der bevarer eksisterende adfærd og tester miljøvariabel + UTF-8 (brug `dotnet`/`git` eller en lille testproces, som de eksisterende tests gør).

## Git-opslag (`GitLocator`)

Rækkefølge; første kandidat der findes **og** svarer på `git --version` med exit 0 inden for 5 sekunder vinder (resultatet caches i procesens levetid; negativt resultat caches ikke):

1. `<AppContext.BaseDirectory>/tools/git/<rid-mappe>/…/git(.exe)` — private pakker (må ikke findes endnu; opslaget skal blot tåle det). `<rid-mappe>`: `win-x64`, `osx-arm64` eller `linux-x64`. Windows-pakkens `cmd\git.exe` prøves før `bin\git.exe`.
2. Windows: `%ProgramFiles%\Git\cmd\git.exe`, `%ProgramFiles(x86)%\Git\cmd\git.exe`, `%LocalAppData%\Programs\Git\cmd\git.exe`. macOS: `/opt/homebrew/bin/git`, `/usr/local/bin/git`, derefter `/usr/bin/git`. Linux: `/usr/bin/git`, `/usr/local/bin/git`.
3. Hver mappe i `PATH` (`git.exe` på Windows, ellers `git`).

`LocalProcessRunner` kræver fuldt kvalificeret sti, så locatoren returnerer altid en absolut sti. Stub-sikkerhed på macOS: `/usr/bin/git` prøves sidst, og `--version`-testen afgør, om den virker. Er der ingen fungerende Git ⇒ `null`. `IGitLocator` er en grænseflade, så tests kan angive en kendt sti eller `null`.

## Git-kommandoer (præcis)

Alle kald går gennem én privat metode `RunGitAsync(workspace, args, ct, maxOutput = default)` som bygger:

```
<git> -C <workspace> --no-pager -c core.quotepath=false -c core.autocrlf=false -c core.safecrlf=false -c commit.gpgsign=false <args…>
```

- `Environment`: `GIT_TERMINAL_PROMPT=0`, `LC_ALL=C`, `GIT_OPTIONAL_LOCKS=0`. `Utf8Output = true`. `Timeout` = 30 sekunder (60 til diff og tilbagerulning). `WorkingDirectory` = arbejdsmappen.
- Alle kald serialiseres med én `SemaphoreSlim(1, 1)` i servicen.
- **Pathspec for dokumenter** (kaldet `MD`): to argumenter efter `--`: `:(glob)*.md`. (`*` krydser ikke `/`, så kun mapperodens filer.)
- Annullering (`ct`) skal give `OperationCanceledException` videre uændret.
- Fejl: ikke-nul exit ⇒ `VersionOperationResult(false, <dansk tekst>)`. Indeholder stderr `index.lock` ⇒ "Et andet program bruger versionsarkivet lige nu. Prøv igen om lidt." Ellers ⇒ "Git-kommandoen fejlede: " + første ikke-tomme stderr-linje (højst 300 tegn). Vis aldrig stakspor.

| Formål | Kommando (efter fælles præfiks) |
| --- | --- |
| Git findes | `--version` (kun via locatoren, uden `-C`) |
| Arkivrod-test | `rev-parse --show-prefix` ⇒ exit ≠ 0: intet arkiv. Tom tekst: mappen er arkivets rod. Ikke-tom tekst: mappen ligger **inde i** et andet arkiv ⇒ `ForkertArkiv` |
| Har commits | `rev-parse --verify --quiet HEAD` (exit 0 = ja). Arkiv uden HEAD behandles som `IkkeAktiveret` |
| Aktivér | (1) hvis intet arkiv: `init`. (2) skriv `.gitignore` hvis den mangler (atomisk, UTF-8 uden BOM, indhold nedenfor). (3) hvis `config --get user.name` er tom: `config user.name Slægtsassistent` og `config user.email slaegtsassistent@localhost`. (4) `config core.autocrlf false`. (5) `add -A -- .gitignore` + `MD` (kun hvis mindst én matchende fil findes; ellers kun `.gitignore`). (6) `commit --no-verify --allow-empty -m "Start versionsstyring"` |
| Status | `status --porcelain=v1 -z --untracked-files=all` + `MD` |
| Historik | `log -n <max+1> --no-color --format=%H%x1f%cI%x1f%s%x1e` |
| Commit alle | Kun hvis status har ændringer: `add -A` + `MD`; derefter `commit --no-verify -m <besked>` (+ identitetsargumenter, se nedenfor) |
| Ændringer (ikke committet) | `add -N` + `MD`; derefter `diff --no-color --no-ext-diff --no-textconv --find-renames HEAD` + `MD` (maks. 8 000 000 tegn output) |
| Ændringer (commit) | `show --no-color --no-ext-diff --no-textconv --format= --find-renames <id>` + `MD` (samme maks.) |
| Tilbagerulning | se nedenfor |

**Identitetsargumenter:** før hver `commit`: hvis `config --get user.name` eller `user.email` er tom, tilføj `-c user.name=Slægtsassistent -c user.email=slaegtsassistent@localhost` som **globale** argumenter foran `commit`. Brugerens egen identitet bruges altid, hvis den findes.

**`.gitignore`-indhold:**

```
# Slægtsassistent versionerer kun persondokumenter (Markdown) i denne mappe.
/*
!/.gitignore
!/*.md
```

Test `git check-ignore` mod filerne `.slaegtsassistent/manifest.json` (ignoreret), `a.md` (ikke ignoreret) og `.a.md.<guid>.tmp` (ignoreret) i en rigtig testmappe.

**Tilbagerulning `RollbackToAsync(workspace, id)`:**

1. Valider `CommitIdValidator.IsValid(id)`; `cat-file -e <id>^{commit}` skal give exit 0, ellers fejl "Versionen findes ikke længere."
2. Hent status. Er der ændringer: `CommitAllAsync(workspace, "Automatisk version før tilbagerulning", ct)`; ved fejl afbrydes uden yderligere ændringer.
3. Hent målets tid og besked: `log -1 --format=%cI%x1f%s <id>`.
4. Sammenlign: `diff --quiet HEAD <id>` + `MD`. Exit 0 ⇒ returnér `Success` med besked "Dokumenterne er allerede i denne tilstand." (ingen commit).
5. `restore --source=<id> --staged --worktree` + `MD`. Ved fejl: forsøg `restore --source=HEAD --staged --worktree` + `MD` og returnér fejl med teksten "Tilbagerulningen mislykkedes; dokumenterne er ført tilbage til den seneste version."
6. `commit --no-verify -m "Rul tilbage til <dd-MM-yyyy HH:mm>: <besked>"` (+ identitetsargumenter). Tiden formateres i lokal tid.

Testen skal bevise, at `restore` i trin 5 (a) gendanner slettede filer, (b) fjerner filer der ikke findes i målversionen, og (c) gendanner ændret indhold byte-for-byte. Fungerer (b) ikke med installeret Git, erstattes trin 5 med `read-tree --reset -u <id>` (index og arbejdstræ) — dokumentér afvigelsen i Validation.md.

## Tilstande og knapper

| Tilstand | Statuslinje i historikvinduet | Knapper |
| --- | --- | --- |
| `GitMangler` | "Git er ikke installeret, så versionsstyring er ikke tilgængelig." + hjælpetekst om at installere Git | Alle skjulte/deaktiverede undtagen **Opdatér** |
| `IkkeAktiveret` | "Versionsstyring er ikke slået til for denne mappe." | **Slå versionsstyring til** |
| `ForkertArkiv` | "Mappen ligger inde i et andet Git-arkiv og kan ikke versioneres her." | Alle deaktiverede |
| `Klar`, 0 ændringer | "Ingen ikke committede ændringer." | Vis ændringer og Commit alle deaktiverede |
| `Klar`, n ændringer | "n ikke committede ændringer (dokumenter). Seneste ændring: dd-MM-yyyy HH:mm." (1 ⇒ "1 ikke committet ændring (dokument)") — "Seneste ændring: tidspunkt ukendt." hvis `LastChangeTime` er null | Vis ændringer og Commit alle aktive |

Hvis `IVersioningHost.HasUnsavedEditors`: tilføj en ekstra linje "Der er ugemte ændringer i editoren. De gemmes automatisk, når du committer." Alle handlinger er deaktiveret når `IsBusy` (en handling kører) eller `host.IsBusyWithImport`. **Rul tilbage her** er deaktiveret for den nyeste version, når der ikke er ikke committede ændringer.

## Flow

Al flowlogik ligger i **én** klasse, `VersioningWorkflow` (`Services/VersioningWorkflow.cs`, ingen Avalonia-typer), så hovedvindue og historikvindue ikke duplikerer den. Konstruktør: `(IVersionControlService, IVersionDialogService, IUserDialogService, IVersionDiffWindowService)`. Metoder, alle med `IVersioningHost host` som første parameter: `Task<bool> CommitAllAsync(host)`, `Task<VersionOperationResult?> RollbackAsync(host, VersionEntry entry)`, `Task<string?> ShowWorkingChangesAsync(host)`, `Task<string?> ShowCommitChangesAsync(host, VersionEntry entry)` (returnerer fejltekst eller `null`). `MainWindowViewModel` og `VersionHistoryViewModel` får workflowet injiceret og indeholder kun kommando-/tilstandslogik.

**Commit (flowet i `VersioningWorkflow.CommitAllAsync`):**

1. Er `IsImporting`: afbryd med besked.
2. Status. `GitMangler`/`ForkertArkiv`: vis `Message` via `IUserDialogService`. `IkkeAktiveret`: `ConfirmAsync("Slå versionsstyring til", "Versionsstyring er ikke slået til for denne mappe. Vil du slå det til nu? Der oprettes en skjult mappe .git, og dine Markdown-dokumenter får en startversion.", "Slå til")`; ved ja: `EnableAsync`; derefter fortsætter flowet kun hvis der er ændringer (ellers meddelelse "Versionsstyring er slået til.").
3. `host.SaveAllEditors()` hvis nødvendigt; mislykkes den ⇒ stop (fejlen vises allerede af `TrySaveAll`).
4. Opdatér status. 0 ændringer ⇒ "Der er ingen ændringer at committe."
5. `AskCommitMessageAsync(CommitMessageSuggester.Suggest(now))`; `null` eller blank ⇒ stop.
6. `CommitAllAsync`; vis fejl ved mislykkes; opdatér historik/status ved succes.

**Rul tilbage her:** `ConfirmAsync("Rul tilbage", "Dokumenterne gendannes til versionen fra <dato tid> (<besked>). Dine nuværende ændringer gemmes først som en ny version, så du kan fortryde. Fortsæt?", "Rul tilbage")` ⇒ `SaveAllEditors()` ⇒ `RollbackToAsync` ⇒ ved succes `ReloadWorkspace()`, derefter opdatér historik/status og vis resultatbesked i vinduet.

**Vis ændringer:** hent diff (commit eller ikke committet) ⇒ `IVersionDiffWindowService.Show(titel, filer, notits)`. Notits ved afkortning: "Visningen er afkortet. Åbn dokumentet eller brug Git til at se resten." Ved `VersionControlException` vises beskeden i historikvinduets fejllinje. Diffvinduet afkorter til 20 000 linjer samlet.

## Hvordan ændringer i hovedvinduet hænger sammen

- `MainWindowViewModel.IsImporting` findes. `HasDirtyEditors` findes. `TrySaveAll()` og `ActivateWorkspace()` findes (`ViewModels/MainWindowViewModel.cs`). `ActivateWorkspace()` rydder editorcache, genindlæser snapshot og dokumentkatalog og vælger første person — den må kun kaldes efter at ugemte editorer er gemt (garanteret af flowet).
- Rejs `DocumentsSaved` fra `TrySaveAll()` efter succes, så et åbent historikvindue opdaterer status.
- Arbejdsmappen er `StandardMarkdownOutputFolder`. Er den tom/ikke sat ⇒ meddelelse "Vælg først en arbejdsmappe i Indstillinger." og ingen kald til servicen. Versionsknapperne er aktive, men viser denne besked.

## Teststrategi

- **Core:** rene tabeltests for de fire parsere og validatoren (se Plan V1). Ingen mocks.
- **App – integration med rigtig Git:** en `GitFact`-attribut (`FactAttribute` med `Skip` sat, hvis `GitLocator` ikke finder Git) bruges til tests der opretter midlertidige mapper under `Path.GetTempPath()` og rydder op i `finally`. Manglende Git registreres som *ikke kørt*, aldrig som succes. Testene må ikke røre brugerens globale Git-konfiguration: sæt `HOME`/`USERPROFILE`/`XDG_CONFIG_HOME`/`GIT_CONFIG_GLOBAL` til en tom testmappe i testprocessens miljø, eller test på en måde hvor identitet mangler.
- **App – ViewModels:** `FakeVersionControlService`, `FakeVersionDialogService`, `FakeVersioningHost` (implementeres i testprojektet). Test observerbar adfærd: kommando kaldt/ikke kaldt, tekster, knapstater, rækkefølge (gem → commit).
- **Headless:** mindst én test pr. nyt vindue, der åbner det med fakes og kontrollerer at bindinger ikke fejler (efter mønstret i `AvaloniaHeadlessRegressionTests`).
- Kør målrettet: `dotnet test tests/SlaegtsAssistent.Core.Tests --filter "FullyQualifiedName~Versioning"` og `dotnet test tests/SlaegtsAssistent.App.Tests --filter "FullyQualifiedName~Version"`. Fuld `dotnet test` ved pakkeafslutning V3, V4 og V5.

## Risici og beslutningsregler

| Risiko | Håndtering |
| --- | --- |
| `git restore` fjerner ikke filer, der ikke findes i målet | Test (b) i tilbagerulningen; fald tilbage til `read-tree --reset -u` |
| Tegnsæt for æøå i stier og beskeder | `core.quotepath=false`, UTF-8-output, test med `Åse Østergård.md` og besked med `æøå` |
| Windows-linjeskift ændres af Git | `core.autocrlf=false` pr. kommando og i arkivet; test round-trip med CRLF-fil |
| Parallelle kald låser `index.lock` | Serialisering, `GIT_OPTIONAL_LOCKS=0` til status, venlig fejltekst |
| Store diff (> 1 MB) | `MaximumOutputCharacters` hævet til 8 mio. for diff; derefter venlig fejl |
| macOS `/usr/bin/git`-stub åbner installationsdialog | Homebrew-stier først; `--version` verificerer; negativt resultat caches ikke |
| Git-hooks eller brugerkonfiguration giver overraskelser | `--no-verify`, `commit.gpgsign=false`, ingen kald mod andre arkiver |
| Vis ændringer ændrer indekset (`add -N` indekserer også sletninger) | Kun indeks, aldrig arbejdstræ; næste `add -A`/`restore --staged` normaliserer. Verificeret manuelt med Git 2.51: status-antal er uændret (`" A"` og `"D "` tælles som Ny/Slettet). Dækkes af test |
