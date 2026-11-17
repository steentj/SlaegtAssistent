# Plan: sprint 8 – brugervenligt desktoparbejdsbord

Status: **påbegyndt; sektion 01, 02, 03, 04 og 05 er implementeret og afventer manuel validering**. Øvrige sektioner er planlagt.

## Mål

Gør de eksisterende kerneopgaver lettere at gennemføre: find en person, skriv og kontrollér biografien, gennemgå GEDCOM-opdateringer, indsæt billeder og eksportér. Appen skal fungere ved den angivne minimumsstørrelse og med tastatur uden tab af brugerdata.

Sprintet omsætter de ti anbefalinger fra GUI-gennemgangen den 17. november 2026. Alle fund er **Inferred**: bygning lykkedes, men appvinduet kunne ikke inspiceres. Klipning, kontrast og faktisk skærmlæseradfærd er ikke observeret. Den ønskede visuelle baseline kunne derfor ikke optages før sektion 01; native prøver og eventuelle justeringer skal dokumenteres.

## Forudsætninger og afgrænsning

Eksisterende dokument-, graf-, versions- og eksportflows er integrationsgrundlag. Planen erklærer ikke tidligere sprints afsluttet; registrér deres åbne fejl før start. Sprintet implementerer ingen AI, RAG eller HTR og ændrer ingen dokumentkontrakter. Den tidligere planlagte AI-sprint 8 flyttes til sprint 9; RAG og HTR til sprint 10 og 11.

## Sektioner

| Nr. | Sektion | Anbefaling | Alvor / indsats | Afhængighed |
| --- | --- | --- | --- | --- |
| 01 | [Tilpas arbejdsbordet til minimumsstørrelsen](01-arbejdsbord-og-vinduesstoerrelser/Plan.md) | R1 | Major / M | Ingen |
| 02 | [Forenkling af forskelsgennemgangen](02-overskuelig-forskelsgennemgang/Plan.md) | R2 | Major / M | Ingen |
| 03 | [Indfør menuer og platformens genveje](03-menuer-og-platformsgenveje/Plan.md) | R3 | Major / M | Ingen; koordiner med 01 |
| 04 | [Gør grafens information tilgængelig uden mus](04-tilgaengeligt-personnetvaerk/Plan.md) | R4 | Major / M | Ingen; fælles navigation samles i 05 |
| 05 | [Saml navigation og skjul grafeksportens detaljer](05-faelles-personnavigation-og-grafeksport/Plan.md) | R5 | Major / M | 01, 03 og 04 |
| 06 | [Gør gemning og versioner forståelige](06-gemning-og-forstaaelige-versioner/Plan.md) | R6 | Minor / S | 03 |
| 07 | [Tilføj handlingsrettede tomme tilstande](07-tomme-tilstande-og-foerstegangsbrug/Plan.md) | R7 | Minor / S | 01 og 05 |
| 08 | [Gør eksportens tilstand og handlinger tydelige](08-tydelig-biografieksport/Plan.md) | R8 | Minor / S | 09 for fælles statusressourcer |
| 09 | [Ret temaressourcer og statusflader](09-semantiske-temaer-og-statusflader/Plan.md) | R9 | Minor / S | Ingen; udføres tidligt som grundlag for 08 |
| 10 | [Forenkling af forhåndsvisning og dansk sprog](10-enkel-forhaandsvisning-og-dansk-sprog/Plan.md) | R10 | Minor / S | 03 |

Indsats S/M er en relativ vurdering. Hver sektion har Plan.md, Requirements.md og Validation.md og kan overtages uden den oprindelige samtale.

## Gennemførelsesrækkefølge

1. Registrér baseline, eksisterende fejl og native screenshots uden at ændre brugerdata.
2. Udfør 09 som fælles temagrundlag, derefter 03 og 01 som menu- og layoutgrundlag.
3. Gennemfør 02 i to reviewbare dele: persongruppering og derefter detalje-/kandidatvisning.
4. Gennemfør 04 og derefter 05; afslut 06, 07, 08 og 10 efter deres afhængigheder.
5. Dokumentér hver sektions prøver, og afslut med sprintets samlede [validering](Validation.md). Afhængigheder styrer rækkefølgen, ikke mappenummeret.

## Arkitektur og færdigkriterier

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

Alle [samlede krav](Requirements.md) og sektionskrav skal have evidens. Platformprøver, kontrastmålinger og brugerflows må ikke tælles som bestået alene ud fra en vellykket bygning. Ingen sektion er afsluttet blot ved oprettelse af disse specifikationer.
