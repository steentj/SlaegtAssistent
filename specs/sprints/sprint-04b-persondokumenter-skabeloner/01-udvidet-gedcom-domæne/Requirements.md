# Requirements: Udvidet GEDCOM-domæne

- GEDCOM-headerens `SUBM`-reference og det tilsvarende submitter-record skal indlæses.
- En person skal kunne relateres til familiebegivenheder, hvor personen er ægtefælle eller anden deltager.
- `MARR` skal kunne indgå i begge ægtefællers personkontekst.
- `CONF` og generiske `EVEN`-poster skal bevares.
- Ukendte eventtags må ikke få parseren til at fejle eller forsvinde uden diagnosticering.
- Eksisterende parseradfærd for kilder, medier, hændelser og census skal bevares.
