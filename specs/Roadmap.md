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

### Trin 2: Grafisk Slægtstræ & Eksport SPRINT 2
*   **Mål:** Opsætte output fra applikationen
*   **Opgaver:**
    *   Udvikling af funktion til at generere Graphviz .dot-filer ud fra GEDCOM-træet. 
    *   Integration af Graphviz-rendering og visning af interaktivt, zoombart slægtstræ (SVG) i appen via Avalonia.Svg.
    *   Implementering af udskriftsfunktion samt eksport af slægtstræ (SVG/PNG) og biografier (PDF/DOCX/ODT) via lokal Python/Pandoc-sidecar.

### Trin 3: Den Lokale AI-Assistent
*   **Mål:** Få hjælp til at skrive prosaen.
*   **Opgaver:**
    *   Etabler C#-forbindelse til en lokal kørende Ollama (f.eks. med en letvægtsmodel) via Microsoft.Extensions.AI.
    *   Tilføj en "AI Berig"-knap i editoren.
    *   Lav et fast system-prompt, der beder AI'en om at omdanne punktforme-facts fra GEDCOM til en pæn, flydende dansk livshistorie.

### Trin 4: Lokale Bøger som Sandhed (RAG)
*   **Mål:** Berig tekst med lokalhistorie uden hallucinationer.
*   **Opgaver:**
    *   Lav en "Kilde-mappe" i appen, hvor brugeren kan smide PDF-filer (f.eks. fra slægtsbibliotek.dk).
    *   Brug `PdfPig` til at gennemsøge disse PDF'er efter personens fødeby, bopæl eller erhverv (f.eks. "Skomager" + "Rye").
    *   Udvælg de 2-3 mest relevante sider, og send dem med som skjult kontekst til Ollama, når der trykkes på "Berig".

### Trin 5: Transkriberings-modulet (HTR)
*   **Mål:** Tyde gammel håndskrift lokalt.
*   **Opgaver:**
    *   Byg et simpelt Python-script, der kan tage et billednavn som argument og køre en lokal TrOCR-model.
    *   I Avalonia-appen: Tilføj et faneblad, hvor brugeren kan uploade et billede af f.eks. en kirkebog.
    *   Appen kalder Python-scriptet i baggrunden, modtager den transkriberede tekst og indsætter den i editoren, så brugeren selv kan rette de sidste fejl.