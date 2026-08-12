# Requirements: Komplet GEDCOM 5.5.1-fortolkning

- Der skal findes en eksplicit mappingtabel for alle records, tags og underfelter i produktets datakontrakt.
- `CONT` og `CONC` skal sammensættes korrekt og deterministisk på alle understøttede tekstfelter.
- Kildehenvisninger under person-, hændelses-, familie- og censusdata skal bevare relevante citationfelter som `PAGE`, `DATA`, `DATE`, `TEXT` og `NOTE`.
- `FAMC`, `FAMS`, `CHAN`, `NOTE`, identitetsfelter og andre strukturtags må ikke fejlklassificeres som hændelser.
- Kendte person- og familiebegivenheder skal klassificeres korrekt; ukendte hændelser skal bevares som ukendte med rå tag og diagnostik.
- GEDCOM-headerens `CHAR` skal respekteres for UTF-8, Unicode, ASCII og ANSEL i overensstemmelse med GEDCOM 5.5.1.
- Uunderstøttet eller modstridende encoding skal give diagnostik uden lydløs tegnkorruption.
- Rå personsegmenter skal bevares nøjagtigt nok til brugerens sammenligning og diagnostik.
- Samme input skal give samme domænemodel og rækkefølge.

Den normative mapping for feature 4.8.5 findes i [Mapping.md](Mapping.md). Tabellen afgrænser udtrykkeligt både bevarede domænefelter og strukturer, som ikke må forveksles med hændelser.
