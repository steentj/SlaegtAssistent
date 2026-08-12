# Requirements: Transaktionel og responsiv import

- Import skal opdeles i forhåndskontrol, gennemgang og gennemførelse.
- Forhåndskontrollen skal fortolke GEDCOM, arbejdsområde, dokumenter, skabelon og kandidater uden vedvarende ændringer.
- Snapshot og dokumenter må først ændres efter vellykket forhåndskontrol og nødvendig brugergodkendelse.
- En fejl eller annullering før gennemførelsen skal efterlade vedvarende data og aktiv UI-tilstand uændret.
- En gennemførelsesfejl skal rulle den planlagte import tilbage eller efterlade en tydelig, gendannelig tilstand.
- Importen må ikke kunne startes parallelt i samme arbejdsområde.
- Langvarig parsing, hashing og fil-I/O må ikke blokere UI-tråden.
- UI skal vise importfase, fremdrift hvor den kan måles, annullering og dansk fejlstatus.
- Snapshot, dokumentkatalog, valgt GEDCOM og personliste skal publiceres som samme importgeneration.
