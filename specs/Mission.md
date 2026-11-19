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
   Nye personer vises som nye med GEDCOM-værdier valgt som standard. En uændret genindlæsning af samme GEDCOM-fil skal ikke skabe ændringsnotifikationer.
3. **Universel Dokumenteksport:** Konvertering af de færdige, redigerede biografier til trykklare PDF'er og redigerbare kontorformater (OOXML/ODF) til nem deling.
4. **Slægtstræ og personnetværk:** Navigér fra en valgt proband i sammenklappelige grene for forfædre og efterkommere, med anenumre efter Kekulé. Eksportér op til seks generationer inklusive rodpersonen som én liggende tabelside i ODT, DOCX eller PDF, eller åbn PDF'en til udskrift. En kendt ane kan vælges som rod for et dybere udsnit. Personnetværket viser øvrige personforbindelser og har sin egen grafeksport.
5. **Lokal AI-Berigelse:** Bruge lokale sprogmodeller (LLM) til at sætte kød på biografierne samt integrere historisk kontekst.
6. **Lokal RAG (Søgning i kilder):** Automatisk gennemsøgning af egne, downloadede lokalhistoriske PDF-bøger (f.eks. fra Danskernes Historie Online) for at berige biografier med historisk præcision baseret på geografi og erhverv.
7. **Lokal Transkribering (HTR):** Hjælpe med at tyde og transkribere indscannede, håndskrevne kilder direkte i appen.

### Persondokumenter og skabeloner
Persondokumenter skal kunne vise de strukturerede GEDCOM-oplysninger, som brugeren vælger:

- begivenheder, herunder fødsel, dåb, vielse, konfirmation, død, begravelse, folketællinger og lægdsruller
- kilder med både lokale henvisninger og en samlet kildeliste
- medier som en oversigt med lokale filhenvisninger; billeder placeres manuelt i dokumentet
- GEDCOM-filens ejer eller afsender (`SUBM`) som valgfri header eller footer

Strukturen og formatteringen styres af en global Markdown-skabelon med sikre felter, betingelser og løkker. Skabelonen må ikke kunne udføre vilkårlig programkode. Ukendte hændelser bevares og kan vises under en samlet kategori for andre hændelser.

### Dokumentation og tekst
**Alle dokumenter, brugerinterface-tekster, kommentarer i koden og anden tekstuel dokumentation skal være på dansk.** Dette sikrer ensartet kommunikation og adgang for målgruppen.

## Data
Input kommer primært i form a GEDCOM filer. Standarden for GEDCOM er defineret [her](https://gedcom.io/specifications/ged551.pdf). Hvis input ikke følger denne standard, skal programmet advare om dette, men ikke afbryde behandling af de øvrige data.

## Afsnit 2: UI/UX-design

### Navigationsprincip
Slægtstræet er applikationens centrale navigationspunkt. Alle andre skærme (biografi, kildesøgning, transskribering, import, indstillinger) tilgås enten via sidebar eller ved at klikke på en person i træet.

- **Personsøgning**: Én fælles personsøgning i navigationen finder *alle* personer i datasættet – ikke kun direkte aner. Søgning og personvalg deles af liste, graf og biografi.
- **Grafvisninger:** Fanen »Graf« viser som standard »Slægtstræ« med proband og nærmeste generationer. Brugeren vælger forfædre, efterkommere eller begge og styrer selv udfoldningen. Underfanen »Personnetværk« giver adgang til den kraftbaserede graf. Se [planen for sprint 8A–8B](Roadmap.md#trin-16--hierarkisk-slægtstræ-og-generationsudskrift-sprint-8a8b).
- Valg i personlisten markerer personen og centrerer personnetværket uden automatisk faneskift. Slægtstræet markerer en allerede synlig forekomst. Enter eller »Åbn biografi« åbner personens dokument. Klik på en person i personnetværket åbner også biografien.
- Navne og direkte forbindelser er tilgængelige ved valg og tastaturfokus; de må ikke kræve hover.

### Biografi-editor
- To-panel-layout: biografiteksten i Markdown til venstre/midt, AI-berigelse i et sidepanel.
- **AI-forslag er ikke synlige som standard.** Brugeren skal aktivt trykke "Bed om AI-forslag", før forslagspunkter vises inde i teksten.
- Når forslag er aktiveret, vises små indsætningspunkter på specifikke steder i teksten (mellem afsnit, ved relevante fakta). Hvert punkt kan foldes ud til at vise et konkret forslag med kildehenvisning og en "Indsæt"-knap.
- AI'en skriver aldrig direkte i teksten – brugeren godkender hvert forslag enkeltvis.

### Øvrige skærme
- **Kildesøgning (RAG)**: fritekstsøgning i lokalt downloadede historiske PDF'er, resultater vist som uddrag med kildehenvisning.
- **Transskribering (HTR)**: split-view med det indscannede billede til venstre og redigerbar tolket tekst til højre.
- **Import**: drop-zone til GEDCOM-filer, med oversigt over indlæste personer og status pr. biografi (ikke startet / under berigelse / klar).
- **Eksport**: valg af biografier og format (PDF, Word/OOXML, ODF), tydelig fremdrift og sikker annullering. Grafeksport samler formatvalg og viser kun relevante indstillinger.
- **Indstillinger**: valg af lokal AI-model, sti til den lokale kildemappe og global persondokumentskabelon.
- **Hjælp**: menuen Hjælp kan åbne et ikke-modalt Markdown-cheat sheet og et ikke-modalt cheat sheet for skabelonformatet. Vinduerne skal kunne stå åbne, mens brugeren arbejder i editoren.

### Designprincipper
- Ingen data forlader maskinen – ingen cloud-kald i UI'et, kun lokale processer.
- Forslag og AI-output er altid tydeligt adskilt fra brugerens egen tekst, og kræver et aktivt klik for at blive en del af biografien.
- Personens frie biografitekst er brugerens autoritative tekst og må aldrig overskrives automatisk af GEDCOM.
- Strukturerede fakta fra GEDCOM og dokumenter vises som en kontrolleret sammenligning, hvor brugeren vælger felt for felt.
- Maskingenereret dokumentindhold holdes i en markeret sektion, så skabelon- og GEDCOM-opdateringer ikke overskriver brugerens frie biografitekst.
- Ændringer i skabelon eller GEDCOM skal skabe en gennemgåelig kandidatopdatering af den maskingenererede sektion. Afviste kandidater må ikke ændre dokumentet.
- Den senest indlæste GEDCOM-fil og personernes rå GEDCOM-segmenter gemmes lokalt, så rådata kan vises ved næste opstart uden ny import.
- Hjælpevinduer er ikke-modale, kan flyttes og lukkes uafhængigt af hovedvinduet og må ikke blokere redigering eller forhåndsvisning.
- Appen skal have et roligt, informationsrigt desktopudtryk med både lyst og mørkt tema samt synlig tastaturfokus.
- Arbejdsbordets editor-, preview- og kontekstpaneler skal kunne tilpasses med en flytbar skillelinje.
- Arbejdsbordet skal kunne betjenes ved 960 × 600 logiske enheder; personlisten kan justeres, og kontekstpanelet kan skjules for at give plads til indholdet.
- Menuer og tastaturgenveje følger platformen. macOS bruger native menulinje og Cmd-genveje; Windows/Linux bruger passende vinduesmenu og Ctrl-genveje.
- »Gem alle« gemmer dokumentfiler; »Gem version…« opretter en lokal version af alle dokumentændringer. Brugeren skal kunne forstå forskellen uden Git-kendskab.
- Førstegangsbrug, tom søgning og manglende personvalg giver hver en tydelig næste handling. Renderet »Forhåndsvisning« er standard; rå HTML er et sekundært teknisk værktøj.
- Forskelle grupperes efter person med tydelige dokument- og importværdier, bevarede feltvalg og mulighed for at se detaljer før godkendelse.
- Farver og fokus er semantiske og tilgængelige i begge temaer; vigtig information må ikke kræve farvesyn eller musebrug.

Disse præciseringer er mål for [sprint 8](sprints/sprint-08-brugervenligt-desktoparbejdsbord/Plan.md), ikke en påstand om allerede implementeret adfærd.
