# Analyse: enklere skabelonredigering

## Problem

Den nuværende skabelon er Markdown blandet med teknisk syntaks som `{{ person.fullName }}`, `{{#if ...}}` og `{{#each ...}}`. Formatet er sikkert og udtryksfuldt, men forudsætter, at brugeren forstår feltnavne, blokke, indlejring og afsluttende markører. Indstillinger viser desuden kun en filsti og et råt tekstpreview. Brugeren kan ikke redigere skabelonen i appen eller se det endelige renderede dokument.

Det giver især disse problemer for uerfarne it-brugere:

- En lille syntaksfejl kan gøre hele skabelonen ugyldig.
- Tekniske engelske feltnavne skal oversættes mentalt til slægtsforskningsbegreber.
- Betingelser og løkker er nødvendige for at undgå tomme afsnit, men er svære at opdage og skrive korrekt.
- Rå Markdown viser ikke tydeligt, hvordan dokumentet ender med at se ud.
- Preview kræver en indlæst person og viser i dag Markdown-tekst frem for samme renderede slutresultat som hovedvinduet.
- Valg af en ekstern fil gør ejerskab, gemning, annullering og gendannelse uklart.

## Undersøgte løsninger

### A. Et nyt, simplere tekstformat

Et dansk format kunne forkorte felter og skjule dele af syntaksen. Det løser dog ikke behovet for valg, gentagelser, tomme data, tabeller og relationer. Brugeren skal stadig lære en syntaks, så den centrale brugerbarriere forsvinder ikke.

### B. Fri WYSIWYG-editor

En fuld dokumenteditor virker umiddelbart enkel, men dynamiske samlinger som hændelser, kilder og medier er ikke almindelig tekst. Det er vanskeligt at vise og redigere gentagne blokke uden skjulte regler. En fuld WYSIWYG-løsning har derfor høj kompleksitet og risiko for at love mere frihed, end rendererens kontrakt kan levere sikkert.

### C. Guidet blokeditor med en struktureret model

Brugeren arbejder med navngivne sektioner som “Fakta”, “Familie”, “Hændelser”, “Kilder” og “Medier”. Sektioner kan slås til og fra, flyttes og konfigureres med almindelige danske valg. Appen gemmer en struktureret, versioneret model og viser resultatet med realistiske data.

Den eksisterende renderer, sikre preview, skabelonhash og kandidatgodkendelse kan genbruges som interne implementeringsdetaljer, hvis det reducerer risikoen. Den tekniske syntaks eksponeres ikke som en del af den nye brugerrejse.

## Beslutning

Sprintet implementerer løsning C. Den strukturerede editormodel bliver den normative kontrakt for nye skabeloner. Der er ikke krav om bagudkompatibilitet, import eller automatisk konvertering af tidligere håndskrevne skabeloner. Ved overgang kan appen tydeligt oplyse, at en ny skabelon skal oprettes eller standardskabelonen anvendes.

Blokeditorens første version dækker de strukturer, som standardskabelonen allerede understøtter:

- dokumenttitel og overskriftsniveau;
- faktafelter og deres rækkefølge;
- forældre, børn og partnere;
- hændelser og folketællinger som liste eller tabel;
- kilder og medier;
- afsenderoplysninger;
- biografiafsnit og fast hjælpetekst;
- frie overskrifter og korte faste tekstblokke uden dynamisk kode.

## Lagringsstrategi

Den indbyggede standardskabelon og alle nye brugerskabeloner gemmes som den versionerede editormodel og kan åbnes igen med samme valg og rækkefølge. Editormodellen placeres i Core og må ikke kende Avalonia. Den omsættes deterministisk til dokumentindhold, eventuelt gennem den eksisterende renderer.

Hvis appen finder en sti til det tidligere tekstbaserede format, skal den vise en kort dansk forklaring og tilbyde at starte med standardskabelonen. Automatisk import, konvertering, avanceret kilderedigering og fortsat understøttelse af det gamle format er uden for sprintets krav.

## Eksempeldata og preview

Preview skal virke uden GEDCOM-import. Et fast, lokalt eksempel indeholder danske navne og repræsentative data for alle sektioner, herunder flere relationer, hændelser, folketælling, kilde og medieplaceholder. Eksempeldata må ikke læses fra netværket og skal være deterministiske.

Hvis der er indlæst personer, kan brugeren i stedet vælge en faktisk person. Preview bruger samme renderer og samme sikre HTML-pipeline som det endelige persondokument. Ændringer vises efter kort debounce, og en renderingsfejl bevarer sidste gyldige preview med en tydelig dansk fejl ved den relevante indstilling.

## Målbar brugerbarhed

Den primære manuelle opgave gennemføres af mindst tre personer, som ikke arbejder professionelt med kode eller skabelonsprog:

1. Åbn skabeloneditoren.
2. Flyt “Kilder” før “Hændelser”.
3. Skjul “Medier”.
4. Vis hændelser som tabel.
5. Kontrollér resultatet i preview.
6. Gem og genåbn skabelonen.

Succeskriteriet er, at mindst 80 % gennemfører uden hjælp på højst fem minutter og uden at se eller skrive skabelonsyntaks. Resultater og observerede problemer registreres i sprintets `Validation.md`.

## Afgrænsning

Sprintet omfatter ikke en generel tekstbehandler, vilkårlig HTML, scripts, brugerdefinerede funktioner, bagudkompatibilitet eller konvertering af det tidligere skabelonformat. Eksportlayout til PDF/DOCX og typografi på tværs af eksportformater hører fortsat til det efterfølgende eksportsprint.
