# Requirements: Importstatus og synkroniseringsbaseline

- En person uden eksisterende dokument skal markeres som `Ny`.
- Nye persondokumenter skal oprettes med GEDCOM-data som valgt standard, uden at brugeren skal bekræfte alle felter i en ændringsdialog.
- En eksisterende person med identisk kanonisk GEDCOM-baseline skal markeres som `Uændret`.
- En eksisterende person med ændret eller ny struktureret GEDCOM-oplysning skal markeres som `Ændret`.
- GEDCOM-felter, der ikke findes i dokumentets ældre baseline, skal behandles som nye oplysninger og have GEDCOM valgt som standard.
- Baselineberegningen skal være deterministisk og uafhængig af rækkefølgen i ordbøger eller andre ikke-deterministiske samlinger.
- En manglende eller ugyldig baseline skal give en tydelig migrerings- eller gennemgangsstatus, ikke en falsk `Ændret`-status uden forklaring.
