# Applikations-konstitution: SlægtsAssistenten
## Afsnit 3: Agile Road Map (Inkrementel Udvikling)

Udvikles i små, afsluttede bidder (sprints), hvor hvert trin resulterer i et funktionelt program.

Trinnumrene er fortløbende. Historiske sprintnavne og mapper (4B–4F) bevares, så eksisterende specifikationshenvisninger fortsat virker. Personlinks er gennemført som sprint 5. Sprint 6 omhandler brugervenlig skabelonredigering. Trin 12 er planlagt som sprint 7A–7C med særskilte leverancer for grafvisning, grafeksport/udskrift og biografieksport.

### Trin 1: Fundamentet (Uden AI)
*   **Mål:** Læs data og tillad manuel redigering.
*   **Opgaver:**
    *   Opsæt Avalonia UI med en simpel to-delt skærm (Venstre: Personliste, Højre: Editor/Preview).
    *   Implementer GEDCOM-indlæsning.
    *   Tilføj UI-flow til at vælge en GEDCOM-fil fra disk og indlæse den i appen.
    *   Generer automatisk en standard Markdown-fil pr. person (f.eks. med fødselsdato, sted, forældre).
    *   Implementer Markdown-editor med "Live Preview"-faneblad.

### Trin 2: PoC-forfinelse & udvidet GEDCOM SPRINT 2
*   **Mål:** Gøre PoC'en hurtigere at arbejde i, tydeligere at gemme i, og mere datadækkende før grafisk træ/eksport.
*   **Opgaver:**
    *   Tilføj personfilter over personlisten for hurtig søgning.
    *   Vis renderet web-preview som standard og tilføj skift (radio/toggle) mellem webvisning og rå HTML.
    *   Tilføj `Fil -> Gem`, der gemmer alle ændrede filer i den aktuelle session.
    *   Vis advarsel ved lukning med ugemte ændringer (Gem / Kassér / Annullér).
    *   Vis hover-pop-up på personlisten med rå GEDCOM-information for personen.
    *   Udvid GEDCOM-indlæsning med `SOUR`, `OBJE`, hændelser (`EVEN` m.fl.) samt `CENS` inkl. relevante underfelter.
    *   Giv UI'et en let modernisering (spacing, typografi, farver og tydelige hover/focus-states).
    *   **Ekstra forslag:** Tilføj statusfelt med aktiv person/fil og gemmestatus.
    *   **Ekstra forslag:** Definér eksplicit mapping-tabel for GEDCOM-tags/sub-tags i sprintens requirements.
    *   **Ekstra forslag:** Gør modernisering målbar via konkrete UI-kriterier.

### Trin 3: Moderne desktop-UI & temaer SPRINT 3
*   **Mål:** Erstatte PoC-layoutet med et sammenhængende, informationsrigt arbejdsbord inspireret af LINQPad og JetBrains dotTrace.
*   **Opgaver:**
    *   Etablér fælles design tokens, tydelige fokus-states og tilgængelige arbejdsflader.
    *   Tilbyd lyst, mørkt og systemstyret tema med lokal persistens.
    *   Redesigner personnavigation, editor, preview og statuslinje uden at ændre eksisterende kerneadfærd.

### Trin 4: Dokumenter før GEDCOM & kontrolleret synkronisering SPRINT 4
*   **Mål:** Gøre eksisterende Markdown-dokumenter til første klasse ved opstart og gøre GEDCOM-opdateringer brugerstyrede.
*   **Opgaver:**
    *   Indlæs persondokumenter fra standardmappen før GEDCOM-filer læses.
    *   Tilføj versionsstyret YAML-frontmatter med stabilt GEDCOM-record-id.
    *   Vis forskelle i strukturerede felter og lad brugeren vælge per felt, om GEDCOM-værdien skal anvendes.
    *   Bevar fri biografitekst og AI-tekst uændret ved synkronisering.

### Trin 5: Skabelonbaserede persondokumenter og komplet GEDCOM-rendering SPRINT 4B
*   **Mål:** Færdiggøre forbindelsen fra GEDCOM-elementer til Markdown og gøre persondokumenternes struktur og formattering brugerdefineret, før eksport og grafisk slægtstræ påbegyndes.
*   **Forudsætning:** Trin 4 er færdigt.
*   **Feature-rækkefølge:**
    1. Udvidet GEDCOM-domæne med familiebegivenheder, `SUBM` og hændelsesklassifikation.
    2. Skabelonkontrakt, loader og validering for en sikker Markdown-baseret DSL.
    3. Normaliseret personkontekst og rendering af hændelser, census, kilder, medier og afsender.
    4. Markerede genererede sektioner og diff-baseret genrendering uden overskrivning af fri tekst.
    5. Indstillinger og brugerflow for valg, nulstilling og forhåndsvisning af global skabelon.
*   **Arbejdsform:** Hver feature udvikles testdrevet med Core- og/eller App-tests. Efter hver feature stoppes udviklingen, og featureens manuelle validering gennemføres, før næste feature må starte.

### Trin 6: Hjælp og cheat sheets SPRINT 4C
*   **Mål:** Gøre Markdown-editoren og skabelonformatet selvforklarende uden at blokere brugerens arbejdsflade.
*   **Forudsætning:** Trin 5 er færdigt, så cheat sheets kan beskrive det faktiske skabelonformat.
*   **Feature-rækkefølge:**
    1. Ikke-modalt Markdown-cheat sheet med søgbar og læsbar reference.
    2. Ikke-modalt skabelon-cheat sheet med felter, betingelser, løkker og eksempler.
    3. Hjælp-menuintegration, vindueslivscyklus og samtidig brug af begge vinduer.
*   **Arbejdsform:** Hver feature udvikles testdrevet med stop for manuel afprøvning efter featureens validering.

### Trin 7: Stabil GEDCOM-synkronisering og arbejdsbord SPRINT 4D
*   **Mål:** Fjerne falske ændringsnotifikationer, bevare rå GEDCOM-data mellem sessioner, gøre arbejdsbordet justerbart og sikre, at skabelon- og GEDCOM-ændringer kan genrenderes uden at overskrive fri tekst.
*   **Forudsætning:** Trin 5 og 6 er færdige.
*   **Feature-rækkefølge:**
    1. Korrekt importstatus og synkroniseringsbaseline for nye, ændrede og uændrede personer.
    2. Lokal persistens af GEDCOM-fil, importmanifest og rå personsegmenter.
    3. Flytbar skillelinje og resizable arbejdsbordspaneler.
    4. Kandidatbaseret genrendering ved ændret skabelon eller GEDCOM-data.
*   **Arbejdsform:** Hver feature udvikles testdrevet med Core- og/eller App-tests. Efter hver feature stoppes udviklingen, og featureens manuelle validering gennemføres, før næste feature må starte. Sprintet afsluttes først efter samlet manuel afprøvning af en genåbnet arbejdsmappe.

### Trin 8: Dataintegritet og komplet synkronisering SPRINT 4E
*   **Mål:** Lukke de dokumenterede integritets-, fortolknings-, synkroniserings- og privatlivsproblemer fra helhedsreviewet af 11. august 2026, så brugerens dokumenter ikke beskadiges, skjules eller ændres uden et tydeligt og kontrolleret valg.
*   **Forudsætning:** Trin 7 er implementeret. Trin 11 må ikke begynde, før trin 8 er godkendt samlet.
*   **Feature-rækkefølge:**
    1. Atomisk dokumentlagring og gendannelse.
    2. Stabil dokumentidentitet og levende arbejdsområdekatalog.
    3. Robust frontmatter, migrering og katalogdiagnostik.
    4. Transaktionel, responsiv importkoordinering.
    5. Komplet GEDCOM 5.5.1-fortolkning inden for produktets datakontrakt.
    6. Fejltolerant import og synlige GEDCOM-diagnostikker.
    7. Komplet kanonisk synkroniseringsbaseline.
    8. Feltstyret konfliktløsning og kandidatgodkendelse.
    9. Valideret skabelonkontrakt, korrekt standardrendering og medier.
    10. Privat og kontrolleret Markdown-preview.
    11. Samlet regressions-, platform- og distributionskvalitet.
*   **Arbejdsform:** Hver feature gennemføres med en eksplicit rød-grøn-refaktorér-cyklus. Fejlende tests skrives før produktionskode. Hver feature leverer et komplet, manuelt testbart brugerflow og afsluttes med dokumenteret manuel godkendelse. Arbejdet stopper efter hver feature, og næste feature må ikke påbegyndes, før brugeren skriftligt har godkendt den foregående.

### Trin 9: GEDCOM-metadata og GEDCOM-hjælp SPRINT 4F
*   **Mål:** Skelne oprettelsesmetadata fra hændelser og gøre GEDCOM-formatet forståeligt i appens hjælp.
*   **Opgaver:** Fortolk `CREA` som oprettelsesdato, udelad private tags som hændelser og tilbyd en ikke-modal introduktion til GEDCOM.
*   **Specifikation:** [Sprint 4F](sprints/sprint-04f-gedcom-metadata-og-gedcom-hjaelp/Plan.md).

### Trin 10: Links mellem persondokumenter SPRINT 5
*   **Mål:** Alle strukturerede personomtaler i genererede Markdown-dokumenter linker til den pågældende persons dokument, når personen har et entydigt dokumentmål i arbejdsområdet eller oprettes i samme import.
*   **Forudsætning:** Dokumentidentitet, kontrolleret synkronisering, skabelonrendering og GEDCOM-metadata fra trin 4–9 er implementeret.
*   **Opgaver:**
    *   Opløs dokumentmål efter GEDCOM-record-id og de faktiske filstier; bevar eksisterende filnavne ved navneændringer.
    *   Render relative Markdown-links for forældre, børn, partnere og øvrige identificerede personreferencer i standardskabelonen og understøttede egne skabeloner.
    *   Undgå forkerte links ved navnesammenfald, manglende dokumenter og dublerede dokumentidentiteter.
    *   Tilbyd opdatering af eksisterende genererede sektioner gennem kandidatgodkendelse; bevar fri tekst og afviste ændringer.
    *   Dokumentér skabelonfelter og verificér import, genåbning, genrendering og sikker forhåndsvisning.
    *   Åbn refererede persondokumenter fra previewets navne med ↗ og editorens personknapper; bevar ugemte tekster og gem ingen UI-markører i dokumentet. [Plan for personnavigation](sprints/sprint-05-personlinks/02-personnavigation/Plan.md).
*   **Specifikation:** [Udførlig implementeringsplan](sprints/sprint-05-personlinks/01-links-mellem-persondokumenter/Plan.md), [krav](sprints/sprint-05-personlinks/01-links-mellem-persondokumenter/Requirements.md) og [validering](sprints/sprint-05-personlinks/01-links-mellem-persondokumenter/Validation.md).

### Trin 11: Brugervenlig skabelonredigering SPRINT 6 – næste sprint
*   **Mål:** En uerfaren it-bruger skal kunne tilpasse persondokumentets opbygning uden at skrive skabelonsyntaks og straks kunne se resultatet i et realistisk eksempeldokument.
*   **Forudsætning:** Den validerede skabelonkontrakt, sikker Markdown-preview og personlinks fra trin 8–10 er implementeret.
*   **Beslutning:** En guidet blokeditor med danske betegnelser bliver den fremadrettede skabelonkontrakt. Den eksisterende renderer kan genbruges som intern implementeringsdetalje, men sprintet stiller ikke krav om bagudkompatibilitet med tidligere håndskrevne skabeloner.
*   **Feature-rækkefølge:**
    1. Definér en UI-uafhængig, versioneret editormodel og deterministisk dokumentgenerering.
    2. Byg en guidet blokeditor, hvor sektioner kan tilføjes, skjules, konfigureres og flyttes uden kendskab til feltnavne.
    3. Tilføj live preview med indbyggede eksempeldata og mulighed for at vælge en faktisk indlæst person.
    4. Implementér sikker lagring, genåbning, annullering, nulstilling og tydelig håndtering af det tidligere format uden krav om konvertering.
    5. Afslut med tilgængelighed, kontekstuel hjælp, brugertest med uerfarne it-brugere og fuld regression af import og genrendering.
*   **Succeskriterium:** En førstegangsbruger kan på højst fem minutter ændre sektionsrækkefølge, skjule en sektion, vælge liste eller tabel og gemme en gyldig skabelon uden at se eller skrive `{{ ... }}`.
*   **Specifikation:** [Analyse og beslutning](sprints/sprint-06-brugervenlig-skabelonredigering/Analyse.md), [udførlig implementeringsplan](sprints/sprint-06-brugervenlig-skabelonredigering/Plan.md), [krav](sprints/sprint-06-brugervenlig-skabelonredigering/Requirements.md) og [validering](sprints/sprint-06-brugervenlig-skabelonredigering/Validation.md).

### Trin 12: Interaktivt personnetværk og eksport SPRINT 7A–7C
*   **Mål:** Navigér i alle personforbindelser gennem en Obsidian-inspireret graf, og del færdige grafer og redigerede biografier i lokale standardformater.
*   **Forudsætning:** Trin 5–11 er færdige, så eksporten arbejder på stabile, brugervenligt konfigurerede persondokumenter med dokumenteret synkronisering.
*   **Status:** Detaljeret planlagt; implementering og validering er ikke gennemført som del af planlægningen.
*   **Sprint 7A – personnetværk:**
    1. Saml alle kendte personer, godkendte familierelationer og entydige Markdown-personlinks i én graf med stabil identitet og diagnostik.
    2. Generér DOT og beregn lokalt kraftbaseret layout med Graphviz `sfdp`; vis grafen i en dedikeret Avalonia-tegnekontrol med zoom og panorering.
    3. Vis kvinder som røde prikker, mænd som blå og ukendt/andet køn som grå. En frit valgt rodperson/proband vises som en lidt større prik med ring; probandvalg skjuler ingen personer eller forbindelser.
    4. Vis navn og eksisterende fødsels-/dødsdatoer ved hover/fokus på personen og alle direkte forbundne personer, suppleret med en fuldstændig tilgængelig nabooversigt.
    5. Åbn personens eksisterende Markdown-dokument ved klik uden at ændre proband eller miste ugemte tekster. Søgning omfatter hele grafen.
*   **Sprint 7B – grafeksport og udskrift:** Eksportér hele grafen eller aktuelt udsnit som SVG, PNG og PDF. Etablér lokal sidecar og private værktøjspakker. Udskriv genererer PDF og åbner systemets PDF-fremviser, hvor brugeren vælger printer.
*   **Sprint 7C – biografieksport:** Eksportér aktuelle, eventuelt ugemte biografitekster som DOCX, PDF og ODT, samlet eller som separate filer, med kontrollerede lokale billeder, interne personlinks, batchstatus og udskriftsflow.
*   **Teknisk beslutning:** Interaktion sker på native noder/koordinater, ikke gennem en statisk SVG som eneste UI. Eksport bruger en fælles grafscene; persondokumenter genrenderes eller gemmes ikke som bivirkning af eksport. Alt arbejde udføres lokalt.
*   **Specifikation:** [Samlet implementeringsvejledning](sprints/sprint-07a-personnetvaerk/Implementeringsvejledning.md), [sprint 7A](sprints/sprint-07a-personnetvaerk/Plan.md), [grafkontrakt](sprints/sprint-07a-personnetvaerk/Grafkontrakt.md), [sprint 7B](sprints/sprint-07b-grafeksport-og-udskrift/Plan.md), [fælles eksportkontrakt](sprints/sprint-07b-grafeksport-og-udskrift/Eksportkontrakt.md), [sprint 7C](sprints/sprint-07c-biografieksport/Plan.md) og [dokumentkontrakt](sprints/sprint-07c-biografieksport/Dokumentkontrakt.md). Hver sprint og hver feature har særskilte krav og valideringsplaner.

### Trin 13: Versionsstyring af persondokumenter SPRINT 7D
*   **Mål:** Brugeren kan versionere alle persondokumenter lokalt med Git gennem et simpelt brugerinterface og rulle tilbage uden at miste historik.
*   **Forudsætning:** Trin 10–11 er implementeret (dokumentkatalog, editorcache og gemning).
*   **Feature-rækkefølge:**
    1. Core-typer og parsere for Git-output.
    2. Git-opslag og lokal versionsservice (init, status, commit, historik, diff, ikke-destruktiv tilbagerulning).
    3. Commit-knap med commitdialog i hovedvinduet.
    4. Versionshistorik-vindue (status, Vis ændringer, Commit alle) og diffvindue.
    5. Tilbagerulning (Rul tilbage her), regression og dokumentation.
*   **Beslutning:** Git CLI findes lokalt (privat pakke, kendte stier eller `PATH`); mangler Git, deaktiveres funktionen tydeligt. Medpakning af Git og automatisk commit før import er opfølgning.
*   **Specifikation:** [Analyse](sprints/sprint-07d-versionsstyring/Analyse.md), [implementeringsvejledning](sprints/sprint-07d-versionsstyring/Implementeringsvejledning.md), [plan](sprints/sprint-07d-versionsstyring/Plan.md), [krav](sprints/sprint-07d-versionsstyring/Requirements.md) og [validering](sprints/sprint-07d-versionsstyring/Validation.md).

### Trin 14: Den Lokale AI-Assistent SPRINT 8
*   **Mål:** Få hjælp til at skrive prosaen.
*   **Opgaver:**
    *   Etabler C#-forbindelse til en lokal kørende Ollama (f.eks. med en letvægtsmodel) via Microsoft.Extensions.AI.
    *   Tilføj en "AI Berig"-knap i editoren.
    *   Lav et fast system-prompt, der beder AI'en om at omdanne punktforme-facts fra GEDCOM til en pæn, flydende dansk livshistorie.

### Trin 15: Lokale Bøger som Sandhed (RAG) SPRINT 9
*   **Mål:** Berig tekst med lokalhistorie uden hallucinationer.
*   **Opgaver:**
    *   Lav en "Kilde-mappe" i appen, hvor brugeren kan smide PDF-filer (f.eks. fra slægtsbibliotek.dk).
    *   Brug `PdfPig` til at gennemsøge disse PDF'er efter personens fødeby, bopæl eller erhverv (f.eks. "Skomager" + "Rye").
    *   Udvælg de 2-3 mest relevante sider, og send dem med som skjult kontekst til Ollama, når der trykkes på "Berig".

### Trin 16: Transkriberings-modulet (HTR) SPRINT 10
*   **Mål:** Tyde gammel håndskrift lokalt.
*   **Opgaver:**
    *   Byg et simpelt Python-script, der kan tage et billednavn som argument og køre en lokal TrOCR-model.
    *   I Avalonia-appen: Tilføj et faneblad, hvor brugeren kan uploade et billede af f.eks. en kirkebog.
    *   Appen kalder Python-scriptet i baggrunden, modtager den transkriberede tekst og indsætter den i editoren, så brugeren selv kan rette de sidste fejl.
