# Fejlklassifikation for GEDCOM-import

## Fatal filfejl

Følgende fejl afbryder hele importen før commit:

- manglende eller ugyldig `HEAD` som første post;
- manglende `TRLR` som sidste post;
- tom eller ulæselig fil;
- ukendt, ugyldigt eller modstridende tegnsæt;
- parserfejl, som ikke kan knyttes sikkert til én post eller ét felt.

Den fatale diagnostik har severity `Fatal`, filsti, relevant tag når det kan bestemmes samt konsekvensen: hele importen blev afbrudt uden ændringer. Den tidligere arbejdsområdetilstand bevares.

## Isolerbar postfejl

Følgende fejl springer kun den berørte level-0-post over:

- `INDI`, `FAM`, `SOUR`, `OBJE` eller `SUBM` uden gyldigt record-id;
- et record-id, som allerede er brugt af en tidligere post;
- en ukendt level-0-post, som produktets datakontrakt ikke kan fortolke sikkert.
- en kendt `NOTE`-, `REPO`- eller `SUBN`-post, som endnu ikke har et selvstændigt domæneobjekt.

Den første post vinder ved dublerede record-id’er. Diagnostikken indeholder startlinje, record-id når det findes, tag, filsti og den konkrete konsekvens. Efterfølgende poster fortolkes fortsat.

## Isolerbar feltfejl

En syntaktisk ugyldig underlinje springes over uden at fjerne den omgivende post. Diagnostikken indeholder linje, omgivende record-id, filsti og konsekvens. Ukendte hændelsestags bevares fortsat som rå tags med en advarsel; de droppes ikke.

## Rapport og godkendelse

Rapporten viser antal importerede poster, importerede poster med diagnostik, oversprungne poster og fatale fejl. En rapport med en isoleret `Error` eller en oversprunget post er delvis og kræver brugerens udtrykkelige accept før gennemførelse. Et afslag ændrer ikke filer, snapshot, personliste, valgt GEDCOM eller editorer.

Efter en accepteret import forbliver rapporten synlig. Den kan filtreres til alle, advarsler eller fejl. En diagnostik med person-record-id navigerer til personen; alle diagnostikker viser filsti og tilgængelig linje/tag.
