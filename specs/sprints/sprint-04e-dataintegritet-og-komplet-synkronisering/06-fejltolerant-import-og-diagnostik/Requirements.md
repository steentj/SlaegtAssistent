# Requirements: Fejltolerant import og synlige diagnostikker

- Parseren skal skelne mellem fatal filfejl og isolerbar post- eller feltfejl.
- En isolerbar fejl skal springe den mindst mulige datadel over og fortsætte med øvrige gyldige records.
- Ingen droppet, erstattet eller ukendt værdi må være lydløs.
- Diagnostik skal mindst indeholde alvorlighed, dansk besked, linje, record-id, tag og konsekvens, når oplysningerne findes.
- Importresultatet skal vise antal importerede, importerede med advarsler, oversprungne og fatalt fejlede poster.
- Diagnostikker skal kunne filtreres og knyttes til relevante personer eller filer i UI.
- En delvis import skal kræve brugerens udtrykkelige accept før commit.
- Den tidligere arbejdsområdetilstand skal bevares, hvis brugeren afviser en delvis import.
- Ældre tests, der kræver total afbrydelse ved enhver ugyldig post, skal opdateres til Missionens fejltolerante kontrakt.
