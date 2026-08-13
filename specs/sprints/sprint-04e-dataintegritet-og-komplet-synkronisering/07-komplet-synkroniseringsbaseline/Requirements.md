# Requirements: Komplet kanonisk synkroniseringsbaseline

- Baseline skal omfatte alle understøttede strukturerede persondata, relationer, person- og familiebegivenheder, census, kilder, citationer, medier og submitterdata.
- Baseline skal være versionsstyret og kunne migreres eller markeres som ukendt.
- Kanonisering skal definere sortering, null/tom-værdi, tekstnormalisering og identitet for gentagne elementer.
- Samme semantiske data skal give samme fingerprint uanset ordbogs- og ikke-betydende inputrækkefølge.
- Betydende rækkefølge må ikke sorteres væk.
- Importeret baseline, senest godkendte GEDCOM-værdier og dokumentets aktuelle synlige værdier skal holdes adskilt.
- En ændring skal kunne opdages, selv om den aktuelle skabelon ikke renderer feltet.
- Manglende eller ugyldig baseline skal give en tydelig migrerings- eller gennemgangsstatus.
- Uændret genimport skal være et dokumenteret no-op.

Den normative baselinekontrakt, kanonisering og afstemning er dokumenteret i [KanoniskBaseline.md](KanoniskBaseline.md).
