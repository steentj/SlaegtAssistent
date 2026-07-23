# Feature: Standardmenu og standardmapper for filvalg

## Scope
Udvid app-shell med en klassisk menulinje og et indstillingsflow for standardmapper, så
GEDCOM-indlæsning altid sker via brugerens aktive filvalg, og outputplacering for genererede
Markdown-filer er eksplicit konfigureret.

## In Scope
- Menulinje med mindst:
  - `Fil`
    - `Indlæs GEDCOM fil`
    - `Indstillinger`
      - Åbner separat indstillingsvindue med:
        - `Standard folder for GEDCOM filer (input)`
        - `Standard folder for markdown filer (output)`
    - `Afslut`
  - `Hjælp`
    - `Generel introduktion`
    - `Om`
- GEDCOM-filvælger åbner i den konfigurerede standard-inputmappe, når den findes.
- Hvis standard-inputmappe ikke er sat, gemmes mappen fra den valgte GEDCOM-fil som ny standard.
- Hvis standard-outputmappe mangler ved indlæsning af GEDCOM, skal brugeren vælge outputmappe
  før selve indlæsningen fortsætter.
- Indstillinger for standardmapper gemmes lokalt og genbruges ved næste app-start.
- Ved succesfuld GEDCOM-indlæsning genereres Markdown-filer til den valgte standard-outputmappe.

## Out of Scope
- Avanceret indstillingsdialog med flere faner.
- Cloud-synk af indstillinger.
- MRU-liste eller "senest brugte mapper".

## Decisions & Context
- **God menu-praksis:** Strukturen følger etableret desktop-konvention (filhandlinger under Fil,
  hjælp under Hjælp). Det er derfor en god og forventelig baseline.
- **Enkelt hovedbillede:** Filindlæsning og mappeindstillinger eksponeres via menuen; hovedbilledet
  holdes fri for ekstra knapper/felter til disse handlinger.
- **Persistens:** Standardmapper lagres i en lokal indstillingsfil under brugerens profil.
- **Testbarhed:** ViewModel holder sig fri af Avalonia-typer og bruger services/interfaces til
  fil- og mappevalg samt informationsdialoger.
- **Ingen tavse fejl:** Hvis outputmappe ikke vælges, afbrydes indlæsning tydeligt uden crash.

## Dependencies
- Kræver feature 03 (GEDCOM-filvalg og indlæsning i UI).
- Bruger outputgenerering fra feature 04 (Markdown-biografi-generering).
- Går forud for feature 06 (Markdown editor/live preview), som forudsætter stabile `.md`-filer.
