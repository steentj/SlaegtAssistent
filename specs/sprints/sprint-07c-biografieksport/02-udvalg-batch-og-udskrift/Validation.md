# Validering: Udvalg, batch og udskrift

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Automatisk: 0/1/100/101 valg, ændret filter, samme navn, casekollision og Windows-reserveret navn.
- Integration: fejlet anden konvertering giver nul publicerede filer; fejlet tredje commit giver præcis to gemte filer.
- Manuel: samlet/separat eksport, overskrivning, annullering i hver fase, dirty buffer og systemfremviser.
- Headless: tastaturvalg, synlige begrundelser, progress og fokus tilbage til eksportknap efter fejl.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | B-06, B-07, B-08 | Ikke registreret | Afventer implementering | Ingen accept endnu |

## Arbejdsjournal 13. november 2026

Valgdialog, dansk søgning med bevarede valg, valg af format og tilstand, oversigt over mål og eksisterende filer, annullering samt udskrivning via PDF-fremviser er implementeret. Snapshot henter åbne editorer efter filsti; ingen Markdown-fil gemmes af eksporten. Batchtjenesten konverterer alle filer før publicering og rapporterer gemte og ikke gemte mål ved skrivefejl eller annullering. Nye, ikke godkendte mål eller nye advarsler før publicering blokerer gemning.

Automatiske tests dækker søgning og bevaret udvalg, headless-dialogens vigtigste kontroller, 101 valg, kolliderende og reserverede navne, konverteringsfejl før gemning, delvis skrivefejl, annullering under publicering og nye advarsler. Pakket macOS-sidecar er prøvet med PDF/DOCX/ODT. Native filvælgere, overskrivningsdialog, udskriftsdialog og hele arbejdsforløbet er endnu ikke manuelt afprøvet; 100-personers udvalg og alle tilgængelighedsscenarier er ikke fuldt kontrolleret.

Supplerende regression: En headless prøve gennem hovedvinduets editorcache bekræfter, at snapshot bruger ugemt tekst og efterlader både Markdown-filens bytes og editorens dirty-status uændret. Atomisk oprettelse uden overskrivning er testet ved at oprette en konkurrerende målfil umiddelbart før publicering. Der er desuden testet 0 og 100 valgte personer samt flere reserverede Windows-filnavne.

## Supplerende integritetskontrol 15. november 2026

Forhåndskontrollen beregner nu et fingeraftryk af hvert kontrolleret HTML-grundlag. Billednavnene i HTML er afledt af billedfilernes SHA-256, så ændrede lokale billeder også ændrer fingeraftrykket. Konverteringen sammenlignes med den gennemgåede version, og en forskel stopper jobbet før publicering; udskrivning stoppes før PDF-fremviseren åbnes. En test ændrer et billede efter gennemgangen og bekræfter, at ingen målfil gemmes. Integrationsprøven med den pakkede macOS-sidecar bekræfter samme fingeraftryk ved samlet eksport i alle tre formater og ved separate filer, der bruger samme billede.

Efter ændringen bestod hele .NET-testpakken på macOS med begge sidecarintegrationer aktiveret: 152 Core-tests og 291 App-tests. Den pakkede sidecar blev anvendt i integrationstesten; Windows og native Linux er fortsat ikke afprøvet af denne kørsel.

15. november 2026: En test med den faktiske atomiske filskriver fremprovokerer fejl ved tredje publicering og bekræfter præcis to gemte mål, ét ikke gemt mål og ingen midlertidige rester. En separat test viser, at fejl under anden konvertering bevarer to eksisterende målfiler byte for byte, og en annullering under første konvertering gemmer ingen mål. De målrettede batchtests bestod på macOS.
