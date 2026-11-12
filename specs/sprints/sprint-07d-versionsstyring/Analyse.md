# Analyse: versionsstyring af persondokumenter

Status: **planlagt**. Der er ikke skrevet produktionskode.

## Ønsket

Markdown-dokumenterne for personer skal versioneres lokalt med Git gennem et simpelt brugerinterface:

1. Et separat **Versionshistorik-vindue** med commits (dato, tid, besked), nyeste øverst. Pr. commit: **Rul tilbage her** og **Vis ændringer**.
2. Øverst i vinduet en statuslinje: om der er ikke committede ændringer, hvor mange, og dato/tid for seneste ændring. Her to knapper: **Vis ændringer** og **Commit alle**.
3. En separat **Commit**-knap i hovedvinduet, som stager og committer alle ændringer i alle dokumenter.

## Er Git den nemmeste løsning?

Kort svar: **Git er det rigtige valg, men ikke fordi det er mindst kode. Den reelle risiko er, at slutbrugeren ikke har Git installeret.**

| Løsning | Kode og risiko | Passer til projektet? |
| --- | --- | --- |
| **Git CLI via proces** (anbefalet) | Lille kode: `ILocalProcessRunner` findes allerede. Commit, log, diff og gendannelse er færdige, gennemprøvede kommandoer. Kræver at `git` findes. | Ja, så længe værktøjet kan findes. Giver senere gratis mulighed for fjernbackup, `git`-værktøjer og import i andre programmer. |
| LibGit2Sharp | Intet eksternt program, men native `libgit2` pr. platform og ikke officielt Native AOT-kompatibel (TechStack kræver AOT). Kræver nye NuGet-pakker og lockfil-ændringer. | Nej – høj risiko for AOT- og pakningsproblemer. |
| Egen snapshot-lagring (kopi af filer + JSON) | Ingen afhængighed og AOT-sikker, men vi skal selv bygge linjediff, historik, atomicitet og fejlhåndtering. Ikke Git, så ingen senere fordel. | Reelt alternativ, hvis "intet installeret" er et hårdt krav. Kræver mere egen kode end Git CLI. |
| Medpakket portabel Git (privat `tools/git/<rid>`) | Samme kode som Git CLI plus pakning (ca. 30–60 MB/platform, GPL-licensopfyldelse). Mønsteret findes for Graphviz (`tools/graphviz`). | Den rigtige endelige distributionsløsning, men et selvstændigt pakkearbejde. |

**Beslutning for dette sprint:** Git CLI bag grænsefladen `IVersionControlService`. Git findes ved opslag i denne rækkefølge: (1) privat pakke `tools/git/<rid>/` ved siden af programmet, (2) kendte systemstier, (3) `PATH`. Mangler Git, er funktionen deaktiveret med en tydelig dansk besked, og intet andet i appen påvirkes. Medpakning af Git er **udenfor dette sprint**, men kræver ingen kodeændring, når den tilføjes (trin 1 i opslaget findes allerede). Bemærk i øvrigt, at macOS' `/usr/bin/git` er en stub, som kan åbne en dialog om installation af kommandolinjeværktøjer – derfor prøves Homebrew-stier først, og `git --version` køres altid som test (se Implementeringsvejledning).

Hvis produktejeren kræver, at appen virker uden installeret Git **før** første udgivelse, tilføjes en ekstra pakningsopgave (privat Git pr. platform) – ellers udgives funktionen som "kræver Git".

## Review af ændringsønsket: huller og beslutninger

| # | Hul eller uklarhed | Beslutning i dette sprint |
| --- | --- | --- |
| 1 | Commit-knappen nævner ingen commit-besked. Git kræver en. | Lille dialog med forudfyldt forslag `Ændringer 12-11-2026 09:23`, som brugeren kan rette. Tom besked afvises. Samme dialog fra begge Commit-knapper. |
| 2 | Ugemte tekster i editoren er ikke på disken, så Git ser dem ikke. | "Commit" og "Commit alle" gemmer først alle åbne editorer (eksisterende `TrySaveAll`) og committer derefter. Mislykkes gemningen, committes der ikke. Statuslinjen viser, hvis der er ugemte editorændringer. |
| 3 | "Rul tilbage" kan betyde `reset --hard`, som sletter historik og senere versioner uigenkaldeligt. | **Ikke-destruktiv tilbagerulning:** nuværende tilstand committes først automatisk, dokumenterne gendannes til den valgte version, og resultatet committes som en ny version `Rul tilbage til <dato tid>: <besked>`. Historikken bevares, og tilbagerulningen kan selv rulles tilbage. |
| 4 | Hvad versioneres? Arbejdsmappen indeholder også `.slaegtsassistent/` med rå GEDCOM-data og indstillinger (privatliv, størrelse, ikke et brugerdokument). | Kun `*.md` direkte i arbejdsmappen (samme afgrænsning som dokumentkataloget). `.slaegtsassistent/` og midlertidige filer holdes ude via `.gitignore` og pathspec. Før hver commit afvises staged stier uden for dokumentomfanget; `.gitignore` committes ikke. |
| 5 | Mappen har ikke nødvendigvis et Git-arkiv. | "Slå versionsstyring til": `git init`, `.gitignore`, lokal identitet og en start-commit. Tilbydes fra Commit-knappen og fra historikvinduet. |
| 6 | Arbejdsmappen kan ligge inde i et andet Git-arkiv (fx brugerens egne projekter). | Rør aldrig et overliggende arkiv. Arkivroden skal være selve arbejdsmappen; ellers vises en forklaring, og funktionen er deaktiveret. |
| 7 | Git kræver brugernavn/e-mail, som en almindelig bruger ikke har sat. | Der sættes en **arkivlokal** identitet (`Slægtsassistent`) ved aktivering. Brugerens globale Git-indstillinger ændres aldrig. |
| 8 | "Hvor mange ændringer" er flertydigt. | Antal **dokumenter** (filer) med ændringer: ændrede, nye og slettede. Ikke linjer. |
| 9 | "Dato/tid for sidste ændring" findes ikke i Git for ikke-committede filer. | Højeste filsystem-ændringstidspunkt blandt ændrede/nye filer. Er alle ændringer sletninger, vises "tidspunkt ukendt". |
| 10 | "Vis ændringer" er ikke defineret. | Et separat, ikke-modalt vindue med filliste og farvet linjediff (tilføjet/fjernet). Samme vindue for en commit og for ikke-committede ændringer. Kun visning, ingen redigering. |
| 11 | Efter tilbagerulning er editorer, personliste, preview og graf forældede. | Efter tilbagerulning genindlæses arbejdsområdet med den eksisterende `ActivateWorkspace()`. Ugemte editorændringer er gemt og committet først, så intet går tabt. |
| 12 | Import, skabelonanvendelse og tilbagerulning kan køre samtidig, eller redigering kan overskrives under genindlæsning. | En fælles arbejdsområdespærring blokerer alle dokumentændringer, import og mappeskift fra editor-gemning til rollback-genindlæsningens afslutning; versionskommandoer serialiseres desuden. |
| 13 | `LocalProcessRunner` begrænser output til 1 MB og bruger systemets tegnsæt. | Runneren udvides bagudkompatibelt med miljøvariabler og UTF-8-output. For stort diff giver en venlig besked i stedet for en fejl. |
| 14 | Historikken kan blive meget lang. | Vis de seneste 500 versioner med en note, hvis der er flere. Ingen sidehentning i dette sprint. |
| 15 | Det store risikopunkt: GEDCOM-import og skabelonanvendelse ændrer mange dokumenter på én gang. | Brugeren kan selv committe før. Automatisk commit før import foreslås som opfølgning (se afgrænsning), ikke en del af dette sprint. |

## Bevidste afgrænsninger (ikke i sprintet)

Grene, merge, remote/push/pull, stash, tags, genskabelse af enkelte filer, sletning af versioner, søgning i historik, automatisk commit, medpakning af Git og versionsstyring af `.slaegtsassistent/`. Remote kræver en særskilt privatlivsvurdering, fordi dokumenterne kan indeholde oplysninger om levende personer.

## Vigtigste designvalg

- **Ny afhængighed: ingen NuGet-pakker.** Ingen ændring af lockfiler. Historikken bruger `ListBox` (ikke `DataGrid`).
- Rene parsere og modeller i **Core** (ingen I/O); proces og UI i **App**. ViewModels er fri for Avalonia-typer.
- Sikkerhed: aldrig shell; kun `ArgumentList`; commit-id valideres mod `^[0-9a-f]{40,64}$`; stier sendes efter `--`; ingen `reset --hard`; ingen kommandoer mod andre arkiver; app-ejet tom `core.hooksPath` for alle Git-kald; konverterende attributter afvises; staged stier uden for dokumentomfanget afvises før commit.
