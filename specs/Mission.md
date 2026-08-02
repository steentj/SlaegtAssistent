# Applikations-konstitution: SlægtsAssistenten
## Afsnit 1: Mission & Vision

### Formål
SlægtsAssistenten er en privatlivsfokuseret desktop-applikation, der skal hjælpe slægtsforskere med at transformere tørre, strukturerede slægtsdata (GEDCOM) til levende, historisk berigede biografier i Markdown-format.

### Målgruppe & Filosofi
*   **Målgruppe:** Udvikleren selv samt venner med moderne hardware (minimum Apple Silicon Mac / Windows pc med 16 GB RAM).
*   **Økonomi:** 100% gratis i drift. Ingen betalingskomponenter, cloud-abonnementer eller API-gebyrer.
*   **Dataprivatliv:** Alle data, slægtshistorier og AI-modeller forbliver lokalt på brugerens egen computer. Ingen personfølsomme oplysninger sendes til tredjepart.

### Kernefunktionalitet
1.  **Struktur til Prosa:** Læse GEDCOM-filer og oprette én redigerbar Markdown-biografi pr. person ud fra en bruger valgt skabelon.
2. **Dokumentførst arbejdsgang:** Indlæse eksisterende persondokumenter lokalt ved opstart, før en GEDCOM-fil læses, og lade brugeren kontrollere alle efterfølgende faktuelle forskelle.
3. **Universel Dokumenteksport:** Konvertering af de færdige, redigerede biografier til trykklare PDF'er og redigerbare kontorformater (OOXML/ODF) til nem deling.
4. **Visuelt Slægtstræ:** Interaktiv grafisk visning af slægtstræet med mulighed for navigation, direkte udskrift
og eksport i højopløselige billedformater.
5. **Lokal AI-Berigelse:** Bruge lokale sprogmodeller (LLM) til at sætte kød på biografierne samt integrere historisk kontekst.
6. **Lokal RAG (Søgning i kilder):** Automatisk gennemsøgning af egne, downloadede lokalhistoriske PDF-bøger (f.eks. fra Danskernes Historie Online) for at berige biografier med historisk præcision baseret på geografi og erhverv.
7. **Lokal Transkribering (HTR):** Hjælpe med at tyde og transkribere indscannede, håndskrevne kilder direkte i appen.

### Persondokumenter og skabeloner
Persondokumenter skal kunne vise de strukturerede GEDCOM-oplysninger, som brugeren vælger:

- begivenheder, herunder fødsel, dåb, vielse, konfirmation, død, begravelse, folketællinger og lægdsruller
- kilder med både lokale henvisninger og en samlet kildeliste
- medier som lokale, relative Markdown-billedehenvisninger
- GEDCOM-filens ejer eller afsender (`SUBM`) som valgfri header eller footer

Strukturen og formatteringen styres af en global Markdown-skabelon med sikre felter, betingelser og løkker. Skabelonen må ikke kunne udføre vilkårlig programkode. Ukendte hændelser bevares og kan vises under en samlet kategori for andre hændelser.

### Dokumentation og tekst
**Alle dokumenter, brugerinterface-tekster, kommentarer i koden og anden tekstuel dokumentation skal være på dansk.** Dette sikrer ensartet kommunikation og adgang for målgruppen.

## Data
Input kommer primært i form a GEDCOM filer. Standarden for GEDCOM er defineret [her](https://gedcom.io/specifications/ged551.pdf). Hvis input ikke følger denne standard, skal programmet advare om dette, men ikke afbryde behandling af de øvrige data.

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
- **Indstillinger**: valg af lokal AI-model, sti til den lokale kildemappe og global persondokumentskabelon.
- **Hjælp**: menuen Hjælp kan åbne et ikke-modalt Markdown-cheat sheet og et ikke-modalt cheat sheet for skabelonformatet. Vinduerne skal kunne stå åbne, mens brugeren arbejder i editoren.

### Designprincipper
- Ingen data forlader maskinen – ingen cloud-kald i UI'et, kun lokale processer.
- Forslag og AI-output er altid tydeligt adskilt fra brugerens egen tekst, og kræver et aktivt klik for at blive en del af biografien.
- Personens frie biografitekst er brugerens autoritative tekst og må aldrig overskrives automatisk af GEDCOM.
- Strukturerede fakta fra GEDCOM og dokumenter vises som en kontrolleret sammenligning, hvor brugeren vælger felt for felt.
- Maskingenereret dokumentindhold holdes i en markeret sektion, så skabelon- og GEDCOM-opdateringer ikke overskriver brugerens frie biografitekst.
- Hjælpevinduer er ikke-modale, kan flyttes og lukkes uafhængigt af hovedvinduet og må ikke blokere redigering eller forhåndsvisning.
- Appen skal have et roligt, informationsrigt desktopudtryk med både lyst og mørkt tema samt synlig tastaturfokus.