# Validering: sprint 7D – versionsstyring

Forudsætning for manuel validering: Git er installeret; en arbejdsmappe med mindst tre persondokumenter (fx efter en GEDCOM-import); en ekstra tom mappe til "ikke versioneret"-prøven. Brug kun syntetiske eller egne testdata.

## Automatiske kontroller (pr. pakke)

| Pakke | Kommando | Forventet |
| --- | --- | --- |
| V1 | `dotnet test tests/SlaegtsAssistent.Core.Tests --filter "FullyQualifiedName~Versioning"` | Alle grønne |
| V2 | `dotnet test tests/SlaegtsAssistent.App.Tests --filter "FullyQualifiedName~Git\|FullyQualifiedName~LocalProcessRunner\|FullyQualifiedName~Graphviz"` | Alle grønne; `GitFact` ikke sprunget over |
| V3–V4 | `dotnet test tests/SlaegtsAssistent.App.Tests --filter "FullyQualifiedName~Version"`, derefter `dotnet test` | Alle grønne |
| V5 | `dotnet build` og `dotnet test` | 0 fejl, alle tests grønne |

Sprunget-over-tests tæller ikke som succes; antal og årsag noteres i Registrering.

## Manuel validering V3 – Commit

1. Start appen; åbn arbejdsmappen. Knapperne **Commit** og **Versionshistorik** er synlige efter **Gem**. ✔/✘
2. Tryk **Commit** i en mappe uden versionsstyring: dialogen "Slå versionsstyring til" vises; vælg **Annullér** ⇒ intet sker, ingen `.git`-mappe. ✔/✘
3. Ret teksten i et dokument uden at gemme, tryk **Commit**, og vælg **Slå til**. ⇒ editoren gemmes før aktivering, `.git` og `.gitignore` oprettes, og `git log` viser "Start versionsstyring" med den indtastede tekst. Fremkald derefter en gemmefejl og bekræft, at hverken arkivet eller startcommitten oprettes/ændres. ✔/✘
4. Ret teksten i et dokument uden at gemme i et aktiveret arkiv. Tryk **Commit**. Dialogen foreslår "Ændringer <dato tid>". Ret beskeden til noget med æøå, og tryk **Commit**. `git log -1 --stat` viser den rettede besked korrekt og filen som ændret; editoren er ikke længere "ugemt". ✔/✘
5. Tryk **Commit** uden ændringer ⇒ besked "Der er ingen ændringer at committe."; ingen ny commit. ✔/✘
6. Tryk **Commit**, og annullér i beskeddialogen ⇒ ingen commit. ✔/✘
7. Start en import og tryk på **Commit** under importen ⇒ knappen er deaktiveret. ✔/✘
8. Tjek at `.slaegtsassistent/` og `.gitignore` ikke er med i `git ls-files`. Stage med `git add -f -- .slaegtsassistent/manifest.json`; aktivering, **Commit** og tilbagerulning skal afvises uden commit, uden ændring af indekset og uden ændring af filen. Afstage manuelt bagefter. ✔/✘
9. Pr. platform (Windows, macOS, Linux efter tilgængelighed): gentag 3–4 og kontrollér, at æøå i filnavn og besked er korrekte, og at `git diff` ikke viser linjeskifts-ændringer. ✔/✘ (angiv platform)
10. Prøv med en mappe inde i et andet Git-arkiv ⇒ forklarende besked, intet ændres i det overliggende arkiv. ✔/✘
11. Skjul Git midlertidigt (omdøb `git` eller start med tom `PATH`) ⇒ forklarende besked, appen fungerer ellers. ✔/✘
12. Installer hooks i arkivet og angiv en global `core.hooksPath` med markør-hooks for `pre-commit`, `prepare-commit-msg`, `commit-msg` og `post-commit`. Aktivér versionsstyring og udfør commits/rollback ⇒ ingen hook-markør oprettes. ✔/✘
13. Tilføj `*.md text eol=lf` og et aktivt konverteringsfilter i attributterne ⇒ versionsstyringen afviser operationen med en dansk forklaring før dokumenter eller indeks ændres. Fjern attributterne, prøv CRLF igen, og kontrollér byte-for-byte round-trip. ✔/✘

## Manuel validering V4 – Historik og Vis ændringer

1. Tryk **Versionshistorik** ⇒ separat vindue. Tryk igen ⇒ samme vindue fokuseres. ✔/✘
2. Listen viser dato, tid og besked; nyeste øverst. ✔/✘
3. Uden ændringer: "Ingen ikke committede ændringer."; **Vis ændringer** og **Commit alle** er deaktiverede. ✔/✘
4. Gem to dokumenter i hovedvinduet (uden commit) ⇒ historikvinduet (åbent) viser "2 ikke committede ændringer (dokumenter). Seneste ændring: <korrekt dato tid>". ✔/✘
5. Opret et nyt dokument, slet et andet ⇒ antallet stiger; nyt og slettet tælles. ✔/✘
6. Ret et dokument i editoren uden at gemme ⇒ ekstra linje om ugemte ændringer. ✔/✘
7. **Vis ændringer** (øverst) ⇒ separat vindue med filliste og farvet diff; nyt dokument vises som tilføjet; sletning som slettet. Antallet i historikvinduet er uændret bagefter. ✔/✘
8. **Vis ændringer** på en ældre version ⇒ kun den versions ændringer; på den ældste (start-commit) ⇒ alle dokumenter som nye. ✔/✘
9. Skift mellem lyst og mørkt tema ⇒ diff og vinduer er læselige. ✔/✘
10. **Commit alle** ⇒ beskeddialog, derefter en ny øverste version; status "Ingen ikke committede ændringer". ✔/✘
11. Tastatur: Tab gennem knapper og liste, Enter aktiverer; skærmlæser-navne findes (kontrollér mindst via Avalonia-udviklerværktøjer eller test). ✔/✘
12. Luk historikvinduet og hovedvinduet ⇒ ingen hængende processer (`git` kører ikke i baggrunden). ✔/✘
13. Stor ændring (fx 500+ dokumenter) ⇒ vinduet forbliver responsivt; eventuelt afkortet visning viser notits. ✔/✘

## Manuel validering V5 – Rul tilbage her

Forberedelse: tre versioner **A** (oprindelig), **B** (dokument X ændret, dokument Y slettet), **C** (nyt dokument Z, X ændret igen). Derefter en ikke committet ændring i X og en ugemt ændring i editoren for dokument W.

1. Vælg **Rul tilbage her** på A ⇒ bekræftelsesdialog nævner dato, tid og besked og forklarer, at nuværende tilstand gemmes først. **Annullér** ⇒ intet sker. ✔/✘
2. Bekræft ⇒ historikken har to nye versioner øverst: "Automatisk version før tilbagerulning" (indeholder X's ikke committede ændring og W's ugemte tekst) og "Rul tilbage til <dato tid>: <besked>". Ingen versioner er forsvundet. ✔/✘
3. Filerne svarer til A: Y findes igen, Z er væk, X har A's indhold (sammenlign med `git diff <A> HEAD -- "*.md"` ⇒ tom). `.slaegtsassistent/` er uændret. ✔/✘
4. Hovedvinduet er genindlæst: personlisten viser dokumenterne fra A, editor/preview viser A's tekst, ingen ugemt-markering, grafen opdateres. ✔/✘
5. Rul derefter tilbage til versionen "Automatisk version før tilbagerulning" ⇒ tilstanden fra før første tilbagerulning (inkl. W's tekst) er genskabt ⇒ tilbagerulning kan fortrydes. ✔/✘
6. **Rul tilbage her** på den nyeste version uden ændringer er deaktiveret. ✔/✘
7. Rul tilbage til en version, hvor dokumenterne allerede er i samme tilstand ⇒ "allerede i denne tilstand", ingen ny version. ✔/✘
8. Rul tilbage og kør derefter en ny GEDCOM-synkronisering/anvend skabelon ⇒ differenceskærmen fungerer og viser forskelle mod den gendannede tilstand (ingen korrupte frontmatter-metadata). ✔/✘
9. Opret versionshistorik B, redigér arbejdsfiler tilbage til bytepræcis tilstand A uden commit, og rul tilbage til A ⇒ ingen automatisk sikkerhedsversion oprettes; test også med en ny fil og en slettet fil. ✔/✘
10. Med forsinket rollback-service prøv at redigere, gemme, importere, anvende skabelon og skifte mappe ⇒ alle handlinger er blokeret fra før editor-gemning til afsluttet genindlæsning. Tekst skrevet før låsen beholdes; ingen input kan gå tabt. ✔/✘
11. Simulér fejl under `restore`, afsluttende commit, timeout og annullering ⇒ appen kontrollerer om committen faktisk lykkedes; ellers genopretter den seneste commit, verificerer filbytes og indeks og genindlæser før låsen frigives. Fremkald også fejl i genopretningen ⇒ fejlbeskeden lover ikke, at dokumenterne er tilbage. ✔/✘

## Samlet sprintaccept

- Alle krav i [Requirements.md](Requirements.md) er dækket af test eller manuel kontrol (angiv ID ved hver).
- Ingen ændring i lockfiler; ingen ny NuGet-afhængighed.
- Eksisterende tests er grønne; Graphviz-flowet er uændret.
- Roadmap, README og CHANGELOG er opdateret (V5).
- Produktejeren har skriftligt godkendt hver pakke og sprintet som helhed.

## Registrering (udfyldes under implementering)

| Pakke | Dato | Test-resultat (antal grønne / sprunget over) | Manuel validering (hvem, platform) | Afvigelser og beslutninger |
| --- | --- | --- | --- | --- |
| V1 | | | | |
| V2 | | | | |
| V3 | | | | |
| V4 | | | | |
| V5 | | | | |
