# Requirements: Sprint 04E – dataintegritet og komplet synkronisering

## Formål

Sprintet adresserer alle handlingskrævende fund i `Miscellaenous/Review_20260811.md`, før grafisk slægtstræ og eksport påbegyndes.

## Produktkrav

- En eksisterende brugerfil må aldrig efterlades tom eller delvist skrevet efter en fejl.
- En person identificeres stabilt via GEDCOM-record-id; navneændringer må ikke oprette en skjult eller konkurrerende biografi.
- Arbejdsområde, dokumentkatalog, åbne editorer og snapshot skal altid referere til samme valgte mappe.
- Ét defekt eller tvetydigt dokument må ikke blokere opstart eller skjule øvrige dokumenter.
- Import skal forberedes uden sideeffekter og først ændre vedvarende data efter brugerens godkendelse.
- Gyldige GEDCOM 5.5.1-data i den understøttede datakontrakt må ikke tabes eller fejlklassificeres.
- Lokale GEDCOM-fejl skal rapporteres og isoleres, så øvrige gyldige records fortsat kan behandles.
- Synkroniseringsbaseline skal dække alle understøttede strukturerede GEDCOM-data, uafhængigt af den valgte skabelon.
- Brugeren skal kunne afgøre strukturerede forskelle felt for felt og gennemgå den deraf følgende dokumentkandidat.
- Fri Markdown- og AI-tekst uden for genererede markører må aldrig ændres af import eller genrendering.
- Skabeloner skal valideres semantisk, og standardrenderingen skal være korrekt, dansk og deterministisk.
- Medier skal opløses sikkert i forhold til GEDCOM-kilde og dokumentmappe; manglende filer skal give synlig diagnostik.
- Markdown-preview må ikke foretage udgående netværkskald eller udføre ukontrolleret aktivt indhold.
- Import og gemning skal holde UI'et responsivt og have tydelig status, cancellation og fejltilstand.
- De lovede målplatforme og distributionsformer skal have reproducerbar build- og smoke-test-evidens.

## Kvalitetskrav

- Al produktionskode udvikles testdrevet med rød-grøn-refaktorér.
- Hver feature skal have Core-, App-, integration- eller headless UI-tests svarende til dens risici.
- Fejltests skal verificere vedvarende data og state før og efter fejlen, ikke kun exceptiontypen.
- Alle fejl og brugertekster skal være på dansk.
- Ingen cloud-, telemetri- eller skjulte netværksafhængigheder må introduceres.
- Eksisterende brugerændringer og dokumenter skal bevares under migrering.

## Sporbarhed til reviewet

| Reviewfund | Behandles i feature |
| --- | --- |
| Ikke-atomisk dokumentlagring | 01 – Atomisk dokumentlagring og gendannelse |
| Navnebaserede dubletter og skjulte dokumenter | 02 – Stabil dokumentidentitet og arbejdsområde |
| Forældet katalog og usikkert mappeskift | 02 – Stabil dokumentidentitet og arbejdsområde |
| Defekt frontmatter kan blokere opstart | 03 – Robust frontmatter og katalogdiagnostik |
| Halvfærdig import og UI-blokering | 04 – Transaktionel og responsiv import |
| Tabte eller fejlklassificerede GEDCOM-data | 05 – Komplet GEDCOM-fortolkning |
| Skjulte advarsler og total afbrydelse ved lokale fejl | 06 – Fejltolerant import og diagnostik |
| Ufuldstændig kanonisk baseline | 07 – Komplet synkroniseringsbaseline |
| Manglende felt-for-felt-konfliktløsning | 08 – Feltstyret konfliktløsning |
| Forkert standardoutput, mediestier og ukendte skabelonfelter | 09 – Skabelonkontrakt, rendering og medier |
| Mulige netværkskald og aktivt indhold i preview | 10 – Privat Markdown-preview |
| Manglende risiko-, UI-, platform- og distributionsverifikation | 11 – Regression, platform og distribution |

## Fast godkendelsesport

- Hver feature skal være færdig på tværs af Core, App, UI, fejlvisning, tests og relevant dokumentation.
- Featureens `Validation.md` gennemføres manuelt og udfyldes med dato, godkender og noter.
- Arbejdet stopper efter hver feature.
- Næste feature må ikke påbegyndes, og der må ikke skrives produktionskode til den, før brugeren skriftligt har godkendt den foregående feature.
- En feature med kendte åbne fejl kan ikke godkendes som færdig.

## Uden for afgrænsning

- Grafisk slægtstræ og eksport fra roadmap trin 5.
- AI-, RAG- og HTR-funktioner fra trin 6–8.
- Nye GEDCOM-skrive- eller eksportfunktioner.
