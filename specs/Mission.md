# Applikations-konstitution: SlægtsAssistenten
## Afsnit 1: Mission & Vision

### Formål
SlægtsAssistenten er en privatlivsfokuseret desktop-applikation, der skal hjælpe slægtsforskere med at transformere tørre, strukturerede slægtsdata (GEDCOM) til levende, historisk berigede biografier i Markdown-format.

### Målgruppe & Filosofi
*   **Målgruppe:** Udvikleren selv samt venner med moderne hardware (minimum Apple Silicon Mac / Windows pc med 16 GB RAM).
*   **Økonomi:** 100% gratis i drift. Ingen betalingskomponenter, cloud-abonnementer eller API-gebyrer.
*   **Dataprivatliv:** Alle data, slægtshistorier og AI-modeller forbliver lokalt på brugerens egen computer. Ingen personfølsomme oplysninger sendes til tredjepart.

### Kernefunktionalitet
1.  **Struktur til Prosa:** Læse GEDCOM-filer og oprette én redigerbar Markdown-biografi pr. person.
2. **Universel Dokumenteksport:** Konvertering af de færdige, redigerede biografier til trykklare PDF'er og redigerbare kontorformater (OOXML/ODF) til nem deling.
3. **Visuelt Slægtstræ:** Interaktiv grafisk visning af slægtstræet med mulighed for navigation, direkte udskrift
og eksport i højopløselige billedformater.
4.  **Lokal AI-Berigelse:** Bruge lokale sprogmodeller (LLM) til at sætte kød på biografierne samt integrere historisk kontekst.
5.  **Lokal RAG (Søgning i kilder):** Automatisk gennemsøgning af egne, downloadede lokalhistoriske PDF-bøger (f.eks. fra Danskernes Historie Online) for at berige biografier med historisk præcision baseret på geografi og erhverv.
6.  **Lokal Transkribering (HTR):** Hjælpe med at tyde og transkribere indscannede, håndskrevne kilder direkte i appen.

### Dokumentation og tekst
**Alle dokumenter, brugerinterface-tekster, kommentarer i koden og anden tekstuel dokumentation skal være på dansk.** Dette sikrer ensartet kommunikation og adgang for målgruppen.

## Afsnit 2: UI/UX-design

### Navigationsprincip
Slægtstræet er applikationens centrale navigationspunkt. Alle andre skærme (biografi, kildesøgning, transskribering, import, indstillinger) tilgås enten via sidebar eller ved at klikke på en person i træet.

- **Personsøgning**: Et søgefelt over slægtstræet kan finde frem til *alle* personer i datasættet – ikke kun direkte aner (fx søskende eller personer der optræder via kildesøgning).
- **Listevisning**: Slægtstræet kan skiftes til en tabel-liste for hurtig scanning af mange personer.
- Klik på en person (i træ eller liste) åbner personens biografi.

### Biografi-editor
- To-panel-layout: biografiteksten i Markdown til venstre/midt, AI-berigelse i et sidepanel.
- **AI-forslag er ikke synlige som standard.** Brugeren skal aktivt trykke "Bed om AI-forslag", før forslagspunkter vises inde i teksten.
- Når forslag er aktiveret, vises små indsætningspunkter på specifikke steder i teksten (mellem afsnit, ved relevante fakta). Hvert punkt kan foldes ud til at vise et konkret forslag med kildehenvisning og en "Indsæt"-knap.
- AI'en skriver aldrig direkte i teksten – brugeren godkender hvert forslag enkeltvis.

### Øvrige skærme
- **Kildesøgning (RAG)**: fritekstsøgning i lokalt downloadede historiske PDF'er, resultater vist som uddrag med kildehenvisning.
- **Transskribering (HTR)**: split-view med det indscannede billede til venstre og redigerbar tolket tekst til højre.
- **Import**: drop-zone til GEDCOM-filer, med oversigt over indlæste personer og status pr. biografi (ikke startet / under berigelse / klar).
- **Eksport**: valg af biografier og format (PDF, Word/OOXML, ODF).
- **Indstillinger**: valg af lokal AI-model og sti til den lokale kildemappe.

### Designprincipper
- Ingen data forlader maskinen – ingen cloud-kald i UI'et, kun lokale processer.
- Forslag og AI-output er altid tydeligt adskilt fra brugerens egen tekst, og kræver et aktivt klik for at blive en del af biografien.