# Requirements: Lagring og fejlhåndtering

- **Gem** og **Gem som** skal skrive atomisk og validere den genindlæste fil før aktivering.
- **Annuller** skal efterlade fil, indstillinger og aktive dokumenter uændrede.
- En skrive- eller valideringsfejl skal bevare den tidligere aktive skabelon.
- Skabeloner i det nye format skal genåbnes med samme editormodel.
- Hvis indstillinger peger på det tidligere format, skal appen forklare, at det ikke understøttes, og tilbyde en ny standardskabelon.
- Der kræves ikke import, konvertering eller fortsat brug af det tidligere format.
- En gemt skabelonændring skal fortsat følge kandidatflowet og må ikke omskrive persondokumenter direkte.

## Opfølgning på afprøvning

- Overskriftfeltet skal bevare fokus, markering og tekstposition under løbende indtastning; bloklisten må ikke genopbygges ved hvert tegn.
- GEDCOM-filvælgeren skal udtrykkeligt tilbyde alle filer, også efter at en skabelon er gemt i samme programsession.
- **Anvend skabelon** skal være tilgængelig for eksisterende persondokumenter uden at åbne eller genindlæse GEDCOM-filen, også efter genstart.
- Genrendering skal bruge dokumenternes godkendte snapshots og aktuelle dokumentfakta. GEDCOM-baseline, snapshotfiler og fri tekst skal bevares.
- Hver dokumentændring skal vises i det eksisterende kandidatflow og kræve godkendelse. Afvisning og annullering må ikke ændre dokumenter eller editorer.
- Ugemt tekst skal indgå i kandidatpreviewet og forblive ugemt efter godkendelse. En skrivefejl skal rulle allerede skrevne ændringer tilbage.
