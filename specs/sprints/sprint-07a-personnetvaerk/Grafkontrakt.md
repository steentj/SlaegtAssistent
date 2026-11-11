# Grafkontrakt for roadmap-trin 12

Status: planlagt. Dette er implementeringskontrakten, ikke en beskrivelse af færdig kode. Identifikatorerne nedenfor er foreslåede nye typer, medmindre de udtrykkeligt beskrives som eksisterende.

## Produktbeslutninger

Grafen er et Obsidian-inspireret netværk med cirkulære prikker og tynde forbindelser. Den er ikke et hierarkisk anetræ. Alle kendte personer, alle sammenhængende komponenter og isolerede personer medtages. Der er ingen skjult generationsgrænse eller automatisk filtrering til probandens slægt.

“Alle links” betyder både eksplicitte familierelationer og entydigt opløselige personlinks i Markdown, herunder links i fri tekst. Et link tæller også som direkte forbindelse fra modtagersiden. En fælles kilde, ens navne eller fælles sted opretter aldrig en forbindelse. Søskende forbindes gennem deres forældre, medmindre et eksplicit personlink forbinder dem direkte. Denne fortolkning gør alle eksisterende personlinks synlige uden at opfinde relationer.

Probanden vælges med søgning og knappen **Brug som proband**. Alle kendte personer kan vælges, også uden dokument. Første gang vælges den aktuelle person, hvis den findes i grafen, ellers første person efter dansk navnesortering og derefter record-id. Den valgte proband gemmes pr. arbejdsområde og genbruges ved genåbning. Et almindeligt klik åbner biografien og ændrer ikke probanden.

## Datagrundlag og prioritet

1. Appen tager et uforanderligt øjebliksbillede af hele arbejdsområdets katalog og åbne editorer. Brug `_allPeople`, ikke den filtrerede `People`-liste. Core modtager data, ikke ViewModels eller filsystemadgang.
2. For et entydigt, gyldigt dokument bruges den åbne editors `CreateDocument()`, ellers `BiographyDocumentParser` på den gemte fil. De eksisterende `BiographyDocumentMetadata.Facts` er autoritative for de felter, som dokumentet repræsenterer. For gamle dokumenter, hvor `RepresentedFields` er null, regnes alle felter i den gamle facts-kontrakt som repræsenterede. En repræsenteret null-værdi er et bevidst tomt felt og må ikke erstattes af ny importdata.
3. Øvrige strukturerede relationer og felter tages fra versionsgyldig `SyncBaseline.Approved`. `Imported` er ikke en erstatning for `Approved`. Afviste ændringer må hverken ændre farve, datoer eller relationer i grafen.
4. For personer uden et autoritativt dokument bruges den publicerede `FamilyTree`. Ved genstart kan den fulde, hashvaliderede, lokalt gemte GEDCOM-kopi indlæses gennem en ny læseoperation på snapshotservicen og `IGedcomLoader`. Den oprindelige `SourcePath` kan være slettet. Den eksisterende `GedcomSnapshot.RawPersonSegments` er utilstrækkelig til at rekonstruere `FAM`-poster. Genindlæsning er læsning til grafen og må ikke starte import, migrering, kandidater eller gemning.
5. Dokumenter uden baseline bidrager med egne facts, kendte forældre og Markdown-links. Manglende relationer må ikke udfyldes med udokumenterede gæt. Vis **Nogle relationer mangler et godkendt datagrundlag** ved et ufuldstændigt grundlag; grafen kan fortsat bruges.
6. Hver person er én node med `RecordId` som stabil nøgle, også ved navnesammenfald og navneændring. En GEDCOM-person med tvetydigt dokumentmål beholdes, men kan ikke navigeres til. Ved dublerede dokumenter uden en entydig GEDCOM-person vises én diagnostisk node med record-id som navn; modstridende dokumentfakta og udgående links bruges ikke. Kendte indgående links/familierelationer beholdes med diagnostik.

Familieoplysninger kan være gentaget i flere godkendte snapshots. Hver persons egne autoritative forældrefelter bestemmer personens forældrekanter, hvis de findes; andre personers `ChildRecordIds` må ikke genindføre en afvist forælder. For en person uden egne forældrefelter bruges godkendte relationer fra øvrige snapshots eller GEDCOM-reserven. Partnerskab kræver en eksplicit familiepost med to kendte partnere; modstridende godkendte udgaver af samme familie-id markeres og bruges ikke til at udlede nye partnerkanter. Entydige egne forældrekanter og Markdown-links bevares. Dette prioritetstilfælde skal testes særskilt.

## Relationer

`PersonGraphEdge` gemmer et normaliseret nodepar `(minId, maxId)` efter ordinal sortering og en liste af `GraphRelation` med type, eventuel retning, familie-id og oprindelse. Der tegnes én linje pr. nodepar, men alle årsager beholdes. Naboskab beregnes som en urettet mængde fra alle årsager.

| Grundlag | Resultat |
| --- | --- |
| Forælder P til barn B | Forbind P og B; gem retningen P → B i relationens data |
| To partnere i en familie | Forbind partnerne; gem familie-id, også ved flere ægteskaber mellem samme par |
| Ét kendt forældreled | Vis den kendte forbindelse uden kunstig ukendt person |
| Markdown-link A → B | Forbind A og B; gem retningen og kildedokumentets identitet |
| Samme relation flere steder | Fjern dubletter af samme årsag; behold forskellige årsager |
| Selvlink | Ingen linje og personen er ikke sin egen nabo; registrér diagnostik |
| Ukendt mål-id, brudt eller tvetydigt link | Ingen opdigtet node eller vilkårlig filåbning; registrér diagnostik |

Markdown udlæses via Markdig-syntakstræ i App, ikke regulære udtryk eller HTML fra previewet. Medtag inline- og referencelinks; spring billeder, kodeblokke, inlinekode og rå HTML over. Genbrug `PersonDocumentNavigation.Resolve` til samme lokale sti- og identitetskontrol som previewet. Udgående links genberegnes fra aktuel buffer efter 300 ms ro; filomdøbning/katalogændring invaliderer berørte mål. Interne preview-URI'er er UI-data og må ikke gemmes i Markdown.

## Model- og servicegrænser

| Placering | Nye typer og ansvar |
| --- | --- |
| `Core/Graph/` | `GraphPersonInput`, `GraphRelation`, `GraphRelationKind`, `PersonGraphNode`, `PersonGraphEdge`, `PersonGraph`, `PersonGraphBuilder`; ren opbygning, deduplikering og naboindeks |
| `Core/Graph/` | `GraphPoint`, `GraphBounds`, `PersonGraphLayout`, `GraphDotWriter`, `GraphPlainParser`; koordinater som `double`, ingen Avalonia-typer |
| `App/Services/` | `IWorkspaceGraphSource`, `WorkspaceGraphSource`; dokumentprioritet, buffer/katalog, links og snapshotreserve |
| `App/Services/` | `ILocalProcessRunner`, `LocalProcessRunner`, `LocalToolLocator`, `IGraphLayoutService`, `GraphvizLayoutService`; lokale processer og annullering |
| `App/ViewModels/` | `PersonGraphViewModel`, `GraphPersonItemViewModel`; søgning, proband, hover/fokus og kommandoer |
| `App/Controls/` | `PersonGraphControl`; tegning, kamera, koordinattransformation, hit-test og pointerinput |
| `App/Views/` | `PersonGraphView.axaml`; kontrol, værktøjslinje, søgning, nabooversigt og fejltilstande |

`PersonGraph` indeholder nodeopslag, normaliserede kanter, naboopslag og diagnostikker. `PersonGraphLayout` indeholder grafrevision, koordinater, grafgrænser og værktøjsversion. Proband og hover er separat visningstilstand: et probandskift skal ikke starte Graphviz eller ændre kanter.

## Layout og koordinater

Brug lokal Graphviz `sfdp`, ikke det hierarkiske `dot`-layout. Det er en kraftbaseret multilevel-algoritme egnet til større netværk ifølge [Graphviz](https://graphviz.org/docs/layouts/sfdp/). Obsidian er [visuel reference](https://obsidian.md/help/plugins/graph); der kopieres ingen kode eller plugin-afhængighed.

Generér sorteret DOT med interne node-id'er `n0`, `n1`, … og separat record-id-opslag. Personnavne og filstier indgår ikke i procesargumenter eller node-id'er. Brug `graph`, `--`, tomme labels, ens faste layoutdiametre, `start=42`, `overlap=prism`, `splines=line`. Kør `sfdp -Tplain <input.dot> -o <output.plain>` gennem `ArgumentList`. Verificér under implementeringen, at den bundne Graphviz-build understøtter valgene; manglende støtte er en værktøjsfejl med dansk besked.

[Plain-formatet](https://graphviz.org/docs/outputs/plain/) giver grafstørrelse og nodekoordinater i tommer. Parseren skal understøtte citerede tokens, invariant decimalseparator, `graph`, `node`, `edge`, `stop`; afvis dublerede/ukendte/manglende node-id'er, ikke-endelige tal, afkortet og urimeligt stort output. Ignorér edge-kurver: UI tegner rette forbindelser fra nodecentrene. Sæt ingen `size` i DOT; kræv og verificér plain-skala 1. Konvertér `x = 96*xInch`, `y = 96*(heightInch-yInch)`, med positiv grafbredde/-højde. En tom graf håndteres uden proces; en enkelt node får en brugbar minimumsramme.

Kameraet bruger `screen = (world - cameraOrigin) * zoom`. En zoomoperation skal bevare verdenspunktet under cursoren. Kør layout uden for UI-tråden, timeout 30 sekunder, `Kill(entireProcessTree: true)` ved annullering/timeout; læs stdout/stderr samtidigt med grænser. Seneste grafrevision vinder. Behold sidste gyldige graf ved fejl. Cache efter topologi og layoutparametre/værktøjsversion; navne, datoer, farver og proband invaliderer kun visning, ikke layout.

## Udseende og interaktion

| Element | Fast krav |
| --- | --- |
| Kvinde (`F`, trimmet og uden hensyn til store/små bogstaver) | Rød, grundfarve `#DC3C4D` |
| Mand (`M`) | Blå, grundfarve `#3478D4` |
| Andet, ukendt eller manglende køn | Grå `#8A8F98`, ingen gæt ud fra navn eller familierolle |
| Almindelig person | Radius 4 DIP ved zoom 1 |
| Proband | Radius 7,2 DIP ved zoom 1 og tydelig ring, samme kønsfarve; sort ring i lyst tema og hvid ring i mørkt tema |
| Prikker ved zoom | Radius skaleres med zoom, men holdes mellem 3 og 10 DIP; proband er altid 1,8 gange almindelig radius |
| Forbindelser | Tynde tematilpassede linjer uden pile; alle kanter indgår også ved hover |
| Tekst | Ingen permanente navne som standard; labels har konstant skærmstørrelse |

Farverne får tematilpasset kant/ring; køn forklares i signatur og tilgængelig tekst, så farve ikke er eneste informationskanal. Fokus har separat tydelig ring. Forbundne noder dæmpes aldrig så meget, at forbindelser forsvinder.

Pointer over en node (eller tastaturfokus på personen) viser navn samt **Født: …** og **Død: …**, kun når datoen findes, både ved personen og ved **samtlige direkte naboer**. Bevar GEDCOM-datotekst som `ABT 1840` og `BET 1800 AND 1805`; opfind ikke præcise datoer. Manglende navn vises som **Unavngiven (record-id)**. Fremhæv de direkte kanter; andenordens naboer får ingen labels.

Labels placeres i et separat overlay uden at ændre grafkoordinaterne. Ved overlap forskydes labels med korte hjælpelinjer. En permanent tilgængelig, rullebar oversigt **Personen og direkte forbindelser** viser alle de samme oplysninger, også når grafen er tæt eller labels ligger uden for viewport. Alle labels indgår; ingen top-N-afkortning. Oversigten holder valgt/fokuseret person efter pointeren forlader prikken, indtil en ny person vælges eller Escape trykkes; de flydende hoverlabels forsvinder ved pointer-exit. Oversigten kan betjenes uden at holde cursoren på prikken.

Venstreklik uden flytning over 4 DIP åbner personens eksisterende Markdown-dokument i den nuværende editor/preview-arbejdsflade. Genbrug `OpenPersonDocument` og editorcachen; udvid indgangen sikkert til record-id-opslag. Kontrollér entydighed og fileksistens igen ved klik. Ved fejl beholdes graf og aktiv editor, og en dansk besked vises. Ingen ny biografi genereres. Klik åbner fanen **Biografi**, og fanen **Graf** bevarer zoom/panorering ved tilbagevenden.

Panorér ved træk på baggrunden eller midterknap. Hjul/trackpad zoomer omkring cursoren; knapper **+**, **−**, **Vis alle**, **Centrér proband** findes. Zoominterval er 0,1–8. Søgning matcher navn/record-id i hele grafen uden at filtrere noder væk. Valg af søgeresultat centrerer/fokuserer og viser oplysninger; Enter eller **Åbn biografi** navigerer. **Brug som proband** er en separat handling. Native søge- og nabolister giver fuld tastaturadgang; undgå tusindvis af skjulte fokusbare UI-elementer.

## Levetid, ydelse og persistens

En afgrænset koordinationsservice tager graføjebliksbilleder ved opstart, afsluttet import, katalogændring og bufferændring. Eksisterende importtransaktion forbliver autoritativ; grafen må ikke læse halvfærdig import. Under import deaktiveres navigation og grafopdatering, og efter afslutning bruges den publicerede revision. Ved arbejdsområdeskift annulleres alt gammelt arbejde og abonnementer frigives.

Gem kun proband-id og formatversion lokalt i `.slaegtsassistent/graph-settings.json` via `IAtomicFileWriter`, kildegenereret JSON-kontekst og stivalidering. Ingen absolutte personstier. Hvis probanden slettes, vælges standarden ovenfor og brugeren får en kort statusbesked. Kameratilstand beholdes i sessionen; den behøver ikke gemmes mellem opstarter.

Mål på mindst Apple Silicon/Windows-pc med 16 GB RAM: 1.000 noder/3.000 kanter vises inden 5 sekunder, og 10.000 noder/30.000 kanter inden 30 sekunder efter start af layout. Panorering/zoom har p95 billedtid højst 33 ms ved 1.000 noder og højst 50 ms ved 10.000. Hover/fokusoplysninger opdateres inden 100 ms på den lille graf og 250 ms på den store. Mål i Release og registrér maskine, værktøjsversion, tre kørsler og median. Naboer med høj grad testes særskilt. Brug spatialt indeks til hit-test, viewport-culling og én tegnekontrol; kanter uden for viewport kan undlades ved tegning, men fjernes ikke fra modellen eller eksporten.

## Afgrænsning

Første version kræver ikke kontinuerlig fysiksimulering, flytning af individuelle noder, egne farver, redigering af relationer, wikilinks eller Obsidian-plugins. Kraftbaseret layout, zoom, panorering og nabolabels er den aftalte Obsidian-lignende oplevelse. Grafeksport og udskrift følger i sprint 7B.
