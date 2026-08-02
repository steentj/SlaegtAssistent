# Applikations-konstitution: SlægtsAssistenten
## Afsnit 3: Agile Road Map (Inkrementel Udvikling)

Udvikles i små, afsluttede bidder (sprints), hvor hvert trin resulterer i et funktionelt program.

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

### Trin 4.5: Skabelonbaserede persondokumenter og komplet GEDCOM-rendering SPRINT 4B
*   **Mål:** Færdiggøre forbindelsen fra GEDCOM-elementer til Markdown og gøre persondokumenternes struktur og formattering brugerdefineret, før eksport og grafisk slægtstræ påbegyndes.
*   **Forudsætning:** Trin 4 er færdigt.
*   **Feature-rækkefølge:**
    1. Udvidet GEDCOM-domæne med familiebegivenheder, `SUBM` og hændelsesklassifikation.
    2. Skabelonkontrakt, loader og validering for en sikker Markdown-baseret DSL.
    3. Normaliseret personkontekst og rendering af hændelser, census, kilder, medier og afsender.
    4. Markerede genererede sektioner og diff-baseret genrendering uden overskrivning af fri tekst.
    5. Indstillinger og brugerflow for valg, nulstilling og forhåndsvisning af global skabelon.
*   **Arbejdsform:** Hver feature udvikles testdrevet med Core- og/eller App-tests. Efter hver feature stoppes udviklingen, og featureens manuelle validering gennemføres, før næste feature må starte.

### Trin 4.6: Hjælp og cheat sheets SPRINT 4C
*   **Mål:** Gøre Markdown-editoren og skabelonformatet selvforklarende uden at blokere brugerens arbejdsflade.
*   **Forudsætning:** Trin 4.5 er færdigt, så cheat sheets kan beskrive det faktiske skabelonformat.
*   **Feature-rækkefølge:**
    1. Ikke-modalt Markdown-cheat sheet med søgbar og læsbar reference.
    2. Ikke-modalt skabelon-cheat sheet med felter, betingelser, løkker og eksempler.
    3. Hjælp-menuintegration, vindueslivscyklus og samtidig brug af begge vinduer.
*   **Arbejdsform:** Hver feature udvikles testdrevet med stop for manuel afprøvning efter featureens validering.

### Trin 4.7: Stabil GEDCOM-synkronisering og arbejdsbord SPRINT 4D
*   **Mål:** Fjerne falske ændringsnotifikationer, bevare rå GEDCOM-data mellem sessioner, gøre arbejdsbordet justerbart og sikre, at skabelon- og GEDCOM-ændringer kan genrenderes uden at overskrive fri tekst.
*   **Forudsætning:** Trin 4.5 og 4.6 er færdige.
*   **Feature-rækkefølge:**
    1. Korrekt importstatus og synkroniseringsbaseline for nye, ændrede og uændrede personer.
    2. Lokal persistens af GEDCOM-fil, importmanifest og rå personsegmenter.
    3. Flytbar skillelinje og resizable arbejdsbordspaneler.
    4. Kandidatbaseret genrendering ved ændret skabelon eller GEDCOM-data.
*   **Arbejdsform:** Hver feature udvikles testdrevet med Core- og/eller App-tests. Efter hver feature stoppes udviklingen, og featureens manuelle validering gennemføres, før næste feature må starte. Sprintet afsluttes først efter samlet manuel afprøvning af en genåbnet arbejdsmappe.

### Trin 5: Grafisk Slægtstræ & Eksport SPRINT 5
*   **Mål:** Opsætte output fra applikationen
*   **Forudsætning:** Trin 4.5, 4.6 og 4.7 er færdige, så eksporten arbejder på stabile, skabelonbaserede persondokumenter med dokumenteret synkronisering.
*   **Opgaver:**
    *   Udvikling af funktion til at generere Graphviz .dot-filer ud fra GEDCOM-træet. 
    *   Integration af Graphviz-rendering og visning af interaktivt, zoombart slægtstræ (SVG) i appen via Avalonia.Svg.
    *   Implementering af udskriftsfunktion samt eksport af slægtstræ (SVG/PNG) og biografier (PDF/DOCX/ODT) via lokal Python/Pandoc-sidecar.

### Trin 6: Den Lokale AI-Assistent
*   **Mål:** Få hjælp til at skrive prosaen.
*   **Opgaver:**
    *   Etabler C#-forbindelse til en lokal kørende Ollama (f.eks. med en letvægtsmodel) via Microsoft.Extensions.AI.
    *   Tilføj en "AI Berig"-knap i editoren.
    *   Lav et fast system-prompt, der beder AI'en om at omdanne punktforme-facts fra GEDCOM til en pæn, flydende dansk livshistorie.

### Trin 7: Lokale Bøger som Sandhed (RAG)
*   **Mål:** Berig tekst med lokalhistorie uden hallucinationer.
*   **Opgaver:**
    *   Lav en "Kilde-mappe" i appen, hvor brugeren kan smide PDF-filer (f.eks. fra slægtsbibliotek.dk).
    *   Brug `PdfPig` til at gennemsøge disse PDF'er efter personens fødeby, bopæl eller erhverv (f.eks. "Skomager" + "Rye").
    *   Udvælg de 2-3 mest relevante sider, og send dem med som skjult kontekst til Ollama, når der trykkes på "Berig".

### Trin 8: Transkriberings-modulet (HTR)
*   **Mål:** Tyde gammel håndskrift lokalt.
*   **Opgaver:**
    *   Byg et simpelt Python-script, der kan tage et billednavn som argument og køre en lokal TrOCR-model.
    *   I Avalonia-appen: Tilføj et faneblad, hvor brugeren kan uploade et billede af f.eks. en kirkebog.
    *   Appen kalder Python-scriptet i baggrunden, modtager den transkriberede tekst og indsætter den i editoren, så brugeren selv kan rette de sidste fejl.