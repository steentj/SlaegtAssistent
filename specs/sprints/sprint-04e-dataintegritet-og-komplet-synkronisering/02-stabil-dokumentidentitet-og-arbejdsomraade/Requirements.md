# Requirements: Stabil dokumentidentitet og levende arbejdsområde

- GEDCOM-record-id er den stabile personidentitet; visningsnavn må ikke afgøre, hvilken eksisterende fil der åbnes eller opdateres.
- En navneændring må ikke automatisk oprette en ny biografifil for en kendt person.
- Et levende arbejdsområdekatalog skal opdateres ved oprettelse, migrering, omdøbning og fjernelse fra kataloget.
- Dokumenter uden match i den seneste GEDCOM skal forblive synlige og uændrede.
- To dokumenter med samme `recordId` skal give en tydelig tvetydig status og må ikke vælges automatisk.
- En eventuel filomdøbning skal være særskilt, brugerbekræftet og atomisk.
- Skift af outputmappe skal behandles som skift af arbejdsområde.
- Dirty editorer skal gemmes, kasseres eller bevare mappeskiftet annulleret, før et nyt arbejdsområde aktiveres.
- Efter et mappeskift må ingen efterfølgende import skrive til den tidligere mappe.
