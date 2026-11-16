# Mediekontrakt for sprint 7E

Status: **planlagt**. Kontrakten fastlægger de valg, implementeringen skal følge.

## Mapper og stiopslag

`DefaultMediaFolder` er valgfri, global og gemmes i eksisterende `AppSettings`. En ny `MediaResolutionContext` skelner mellem mediemappe, oprindelig GEDCOM-mappe, dokumentets faktiske mappe og kontekstrevision. Dokumentmappen må ikke udledes af en forventning om, at alle filer har et nyt standardfilnavn.

Fælles `LocalMediaPathPolicy` i Core fastlægger normalisering og containment. Filsystemadgang ligger bag `ILocalMediaFileSystem`, som App implementerer, inklusive fysisk opløsning af symbolske links. `BiographyMediaResolver` tilpasses samme kontrakt. Preview og eksport genbruger politikken uden at hente UI-typer ind i Core. Eksisterende understøttede Markdown-billeder må ikke miste formater, blot fordi vælgerens formatudvalg er smallere.

Opslag udføres i denne rækkefølge:

1. Genkend først Windows-drev og separatorer også på macOS/Linux, så et drevbogstav ikke forveksles med et URI-skema. Afvis derefter tom sti, URI-henvisninger (inklusive `file:`) og netværksstier (UNC, også skrevet med `/`). En udenlandsk drevsti behandles som en importeret reference og åbnes aldrig direkte på den aktuelle platform.
2. For en relativ sti: prøv mediemappe + original relativ struktur, hvis mediemappe er sat. Ellers prøves GEDCOM-mappe + struktur. Et direkte eksisterende mål inden for en tilladt fysisk rod har forrang. Hvis en mediemappe er sat og det direkte mål mangler, prøves også det eksisterende GEDCOM-relative mål; resultatet skal angive den anvendte rod.
3. Absolutte stier kan bruges direkte, hvis de er læsbare og fysisk ligger i medie-, GEDCOM- eller dokumentrod. Stier udenfor åbnes ikke.
4. Ved manglende mål og valgt mediemappe: slå op i et afgrænset indeks af filer under den rod. Sammenlign først den længste matchende afslutning af sti-komponenter (fx `familie/foto.jpg`); mindst to komponenter prioriteres over filnavn alene. Hvis ingen sådan afslutning findes, bruges filnavn alene **kun ved ét entydigt match**. Flere matches på det bedste niveau giver Tvetydig, også hvis billedernes indhold er ens. Vis original reference og kandidater; ingen automatisk beslutning. Direkte mål fra trin 2/3 har forrang over dette opslag.
5. Kontrollér den fysiske sti, læsbarhed og tilladt rod igen før læsning/indsættelse. Et link til en fil udenfor roden afvises. Indeksering følger ikke symbolske mapper ud af roden eller i løkker. Der søges aldrig udenfor de valgte rødder eller på netværket.
6. Generér en segmentvis URI-kodet relativ Markdown-sti med `/` fra dokumentmappen. `..` er tilladt, når det fysiske mål ligger inden for en tilladt rod. Et resultat, der stadig er absolut, er ikke indsætteligt; giv besked om at lægge mapperne på samme drev.

Det præcise, eksisterende filnavn bruges; der foretages ingen kulturbestemt lowercasing eller Unicode-omskrivning. Matching følger filsystemets casefølsomhed; forskelle, som giver flere kandidater, behandles som tvetydighed. Test både casefølsomme og caseufølsomme filsystemabstraktioner.

Resultattypen skal skelne mellem Fundet, Mangler, Tvetydig, UdenforTilladtMappe, UgyldigSti, Ulæselig, IkkeUnderstøttet og ForStor. Danske statusmeddelelser indeholder en forståelig næste handling. Brug fx status-id `Unreadable` med teksten "Filen kan ikke læses".

## Katalog og miniaturevælger

- Katalogkilden er godkendte kanoniske snapshots fra dokumenterne og aktuelle editorers metadata. Ved dokumenter uden snapshot bruges eksisterende godkendte persondata, hvis de findes. Mangler begge, vises tom tilstand; ny GEDCOM-import kræves ikke for allerede gemte snapshots.
- Personens medier vises som standard. Alle refererede medier betyder unionen af godkendte referencer i det aktive arbejdsområde. Kilde-/hændelsesmedier indgår kun i det omfang, de allerede er del af den eksisterende normaliserede personmedieliste; sprintet udvider ikke GEDCOM-parserens relationer.
- Medie-id bruges, når det findes. Inline-medier får en deterministisk nøgle af reference og ejeridentitet. Identiske fysiske filer kan samles visuelt med bevarede ejerreferencer; samme titel/filnavn er ikke tilstrækkelig identitet.
- Galleri: miniature op til 160 × 120 logiske pixels, titel, filnavn og status. Suppler med større forhåndsvisning, original reference og opløst sti for den valgte post. Billeder beskæres ikke; EXIF-orientering respekteres. Søgning omfatter titel og filnavn.
- Dialogtitel afhænger af formålet: "Vælg topbillede" eller "Indsæt billede i livsforløbet". Ét valg ad gangen, redigerbar alternativ tekst, valgfri billedtekst, knapper Annullér og Vælg/Indsæt. Ingen ændring før bekræftelse.
- Manglende, tvetydige, ulæselige og ikke-understøttede poster bliver stående med status og pladsholder. De kan markeres for at læse forklaringen, men ikke bekræftes. Tom tilstand linker til Indstillinger via en kommando; åbning annullerer det aktuelle valg.
- PNG/JPEG er indsættelige efter kontrol af signatur og vellykket dekodning; maks. 20 MiB og 40 mio. pixels. Originalen ændres aldrig. Alternative tekster og billedtekster escapes som Markdown; rå HTML må ikke injiceres.
- Højst 60 kort pr. side, højst to samtidige miniaturejobs og højst 64 MiB miniaturecache i hukommelsen. Miniaturer dekodes til målformat uden en vedvarende fuldopløsningsbitmap pr. post. Bitmaps ejes og frigives af App-visningslaget. ViewModels indeholder nøgler, metadata og status, ingen Avalonia-typer.
- Cache er sessionslokal; nøgle omfatter fysisk sti, filstørrelse og ændringstid. Opdatér gennemtjekker metadata og rydder berørte poster. Ingen miniaturefiler skrives til medie- eller dokumentmappe.
- Et rodindeks bygges på baggrundstråd, højst én gang pr. kontekstrevision, med afbrydelse. Over 100.000 filer stoppes med en konkret besked om at vælge en smallere mediemappe. Fejl i undermapper rapporteres; de må ikke skjules som et komplet indeks eller bruges til at bevise entydighed.

## Dokumentejerskab og topbillede

Topbilledet gemmes som almindelig Markdown i præcis én valgfri blok uden for den genererede sektion:

```markdown
<!-- SlaegtsAssistent:portrait:start -->
![Portræt af Anna Jensen](../Medier/anna.jpg)

Anna Jensen omkring 1930.
<!-- SlaegtsAssistent:portrait:end -->

<!-- SlaegtsAssistent:generated:start -->
# Anna Jensen
…
<!-- SlaegtsAssistent:generated:end -->
## Biografi
…
```

Markørnavnene er tekniske identifikatorer og følger det eksisterende mønster. Kommentarerne må ikke vises i preview eller eksport. Rå HTML forbliver deaktiveret.

- Første indsættelse sker ved begyndelsen af dokumentets body, efter frontmatter. Topbilledet ligger før personens titel, hvis titlen indgår i den genererede sektion. Eksisterende body forskydes uden omskrivning.
- Skift topbillede erstatter kun indholdet mellem ét gyldigt markørpar. Fjern topbillede fjerner kun denne blok og dens eget separatorlinjeskift. Ingen originalfil slettes.
- Flere, omvendte, overlappende eller uafsluttede par giver fejl og ingen ændring. Vælgeren forsøger ikke at gætte, hvilket eksisterende Markdown-billede der er et portræt.
- Én blok indeholder ét billede og eventuel billedtekst. Manuel råtekst med flere billeder eller flyttede markører kan læses som almindelig Markdown, men de strukturerede Skift/Fjern-kommandoer afviser en ugyldig blok med hjælp til rettelse.
- En `BiographyImageEditService` i Core foretager tekstoperationerne og returnerer ny body, ny markørposition og diagnostik. Ingen filskrivning, ingen ændring af frontmatter eller sync-/templatehash.
- Et billede i topblokken må gerne også optræde i livsforløbet, hvis brugeren selv indsætter det dér. Begrænsningen på ét gælder topblokken, ikke hele dokumentet.

## Indsættelse og samtidighed

`EditorSelection` bruger UTF-16-positioner som Avalonia-tekstfeltet, inklusive start/slut og bufferrevision. En adapter i View holder markør og markering synkroniseret og gendanner fokus efter indsættelse. Linjeafslutninger CRLF/LF bevares.

Et gyldigt indsættelsesområde ligger helt udenfor den genererede sektion og topbilledblokken. I et normalt dokument er det livsforløbet efter generated:end. I ældre dokumenter uden genereret blok er fri body redigerbar, bortset fra topblokken. Ødelagte genererede markører giver fejl, ingen indsættelse. Markør i fakta medfører beskeden "Placér markøren i livsforløbet nederst i dokumentet"; der flyttes ikke automatisk tekst.

Indsæt en selvstændig billedparagraf med nødvendige tomme linjer, valgfri separat billedtekst og fortsat markør efter indsættelsen. En valgt tekst erstattes kun inden for det gyldige område. Anden tekst bevares tegn for tegn.

Ved åbning af vælgeren fastholdes dokumentsti, person-id, bufferrevision, markering og mediekontekstrevision. Ved bekræftelse kontrolleres alle igen samt at filen stadig kan læses. Ændret kontekst giver besked og kræver genåbning; der indsættes aldrig i en anden person. Import, rollback og arbejdsområdeskift spærrer billedkommandoerne via appens eksisterende spærring. Dialogen kan være modal til hovedvinduet; asynkrone jobs skal stadig beskyttes mod forældede resultater.

Succes ændrer kun aktuel `MarkdownText`, sætter `IsDirty`, opdaterer preview og placerer markøren. Eksisterende Gem, lukkeadvarsel, eksport af ugemte buffers og Commit gælder fortsat. Direkte bufferændring skal håndteres som én fortrydelseshandling i editorens tekstfelt; native fortryd og gentag afprøves eksplicit.

## Liste/tabel, preview og eksport

Medieblokken genererer en liste eller tabel med de valgte metadatafelter, aldrig billedsyntaks. Lokal fil vises som læsbar tekst; titel og sti escapes også i tabelceller. Manglende filer forbliver synlige med status. Fravalg af medieoversigten skjuler ikke brugerens topbillede eller livsforløbsbilleder.

Tidligere `Standard` på medieblokken accepteres som liste ved kompilering, men tilbydes ikke ved nye valg. Ingen modelversionsstigning er nødvendig, hvis eksisterende v1-filer fortsat kan indlæses uændret; test dette. En eventuel nødvendig versionsændring skal dokumenteres med eksplicit migration, før implementeringen fortsætter.

Appen leverer et fælles snapshot af tilladte rødder til importdiagnostik, skabeloneksempler, editorpreview, eksport-preflight og selve konverteringen. Rødder er den faktiske dokumentmappe, GEDCOM-kildemappen og den gemte mediemappe. Et mappeskift opdaterer alle åbne previewkontekster uden at nulstille deres buffers; gamle rødder fjernes. Ingen dokumenter omskrives eller gemmes som bivirkning.

Preview-CSP og offline eksport bevares. Indsatte filer eksporteres gennem den eksisterende ressourceindsamler, med samme størrelsesgrænser og samlet eksportbudget. En fil, der forsvinder efter valg, giver eksisterende kontrolleret advarsel; selve Markdown-henvisningen slettes ikke. Mediemappens adgang betyder adgang til lokale filer under den rod, aldrig til forældremappen som helhed.
