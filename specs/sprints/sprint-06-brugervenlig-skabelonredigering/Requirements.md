# Requirements: Sprint 6 – brugervenlig skabelonredigering

## Produktkrav

- Standardvejen til skabelontilpasning skal kunne bruges uden at skrive eller forstå skabelonsyntaks.
- Brugeren skal kunne vise, skjule og ændre rækkefølge på de understøttede dokumentsektioner.
- Relevante sektioner skal kunne vises som liste eller tabel, når datamodellen understøtter det.
- Alle felter og valg i den guidede editor skal præsenteres med danske navne og korte forklaringer.
- Preview skal virke med lokale eksempeldata uden en indlæst GEDCOM-fil.
- Brugeren skal kunne skifte preview til en faktisk indlæst person, når en sådan findes.
- Preview skal svare til den tekst og sikre HTML, som produktionsrenderingen ville skabe for samme model og person.
- **Annuller** må ikke ændre filer, aktiv skabelonsti, dokumenter eller kandidater.
- **Gem** må først aktivere en skabelon, når den er skrevet, genindlæst og valideret med succes.
- Nulstilling skal kræve bekræftelse og skal kunne forhåndsvises før gemning.
- Hvis appen finder det tidligere skabelonformat, skal den tydeligt forklare, at brugeren skal oprette en ny skabelon eller anvende standarden.
- Skabelonændringer må ikke automatisk overskrive eksisterende persondokumenter eller fri biografitekst.

## Tekniske krav

- Editormodellen og dens compiler skal ligge i Core uden Avalonia-afhængigheder.
- ViewModels skal være fri af Avalonia UI-typer og kunne testes uden renderer.
- Den nye editormodel skal være den normative kontrakt for brugerens skabelonvalg.
- Skabeloner i det nye format skal have en modelversion og kunne genåbnes deterministisk.
- Samme editormodel skal altid give byte-identisk skabelontekst.
- Eksempeldata skal være deterministiske, lokale og uden personoplysninger fra brugeren.
- Previewopdateringer skal kunne annulleres og må ikke blokere UI-tråden ved gentagne ændringer.
- Filskrivning skal være atomisk og må ikke efterlade en delvist skrevet skabelon som aktiv.
- Fejl skal være på dansk og, hvor muligt, pege på den berørte blok eller indstilling frem for kun linje og kolonne.
- Ingen cloudkald, telemetri, eksterne billeder eller andre netværksafhængigheder må indføres.

## Tilgængelighed og brugerbarhed

- Hele den guidede editor skal kunne betjenes med tastatur.
- Fokus, valgt blok, fejltilstand og deaktiverede valg skal kunne skelnes uden kun at bruge farve.
- Kontroller skal have tilgængelige danske navne og logisk tab-rækkefølge.
- Editorens kernefunktioner skal fungere ved minimumsvinduestørrelsen og ved 200 % skalering uden skjulte gemme- eller annulleringshandlinger.
- Mindst 80 % af de planlagte uerfarne testbrugere skal gennemføre hovedopgaven på højst fem minutter uden hjælp.

## Afgrænsning

- Der kræves ingen bagudkompatibilitet, import eller konvertering af tidligere skabeloner.
- Der indføres ikke et nyt offentligt skabelonsprog.
- Sprintet ændrer ikke GEDCOM-fortolkningen eller dokumenternes synkroniseringsbaseline.
- Sprintet omfatter ikke sidelayout eller eksport til PDF, DOCX eller ODT.
