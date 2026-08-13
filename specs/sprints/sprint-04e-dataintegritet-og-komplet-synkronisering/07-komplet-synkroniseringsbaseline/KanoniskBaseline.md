# Kanonisk synkroniseringsbaseline

## Versioner og tilstande

- Markdown-formatversion 3 introducerer `syncBaseline`.
- Baselineformatets aktuelle version er 1.
- `imported` er den komplette kandidat fra den aktuelle GEDCOM-import.
- `approved` er den seneste komplette GEDCOM-tilstand, som brugeren har godkendt.
- `facts` er dokumentets aktuelle, synlige strukturerede værdier og holdes adskilt fra begge GEDCOM-tilstande.
- Nye dokumenter starter med identiske `imported`- og `approved`-snapshots.
- En godkendt kandidat gemmer det nye komplette snapshot som både importeret og godkendt. En afvist kandidat ændrer ingen af dem.

Dokumenter fra formatversion 1–2 migreres til formatversion 3 uden at opfinde en baseline. `syncBaseline: null` giver status **Baseline mangler** og kræver manuel gennemgang. En ukendt baselineversion bevares læsbar, men giver status **Ukendt baselineversion** og må ikke automatisk accepteres.

## Felter i snapshotversion 1

Snapshotet omfatter:

- personens record-id, navn, køn, fødsel, død og noter;
- forældre- og børnereferencer;
- personens familie-id’er, ægtefæller, børn og familienoter;
- person- og familiehændelser med tag, kategori, værdi, dato, sted, type, note og citationer;
- census med dato, sted, note og citationer;
- direkte person- og familiekilder samt alle kildecitationfelter;
- medier med record-id, fil, format, titel, type og note;
- submitterens record-id, navn, adresse, telefon, e-mail, websted og sprog.

## Kanoniseringsregler

| Datatype | Regel |
| --- | --- |
| Tekst | Unicode normaliseres til NFC. `CRLF` og `CR` normaliseres til `LF`. `null`, tom og ren whitespace repræsenteres ens som `null`. Anden tekst og betydende indre whitespace bevares. |
| Objektfelter | Serialiseres i den faste rækkefølge, som snapshotkontrakten definerer. |
| Forældre, børn og familier | Record-id’er behandles som mængder: dubletter fjernes og sorteres ordinalt. Familier sorteres efter record-id. |
| Direkte kilder og citationer | Behandles som ikke-ordnede gentagelser og sorteres efter stabil indholdsidentitet og derefter record-id og side. |
| Hændelser | Inputrækkefølgen er betydende og bevares. Hver hændelse har en stabil indholdsidentitet. |
| Census | Inputrækkefølgen er betydende og bevares. Hvert element har en stabil indholdsidentitet. |
| Noter og medier | Inputrækkefølgen er betydende og bevares. Medier har en stabil indholdsidentitet. |
| Fingerprint | SHA-256 over UTF-8 af den kanoniske JSON-repræsentation, vist som store hextegn. |

Samme semantiske input giver dermed samme fingerprint på tværs af ordbogsorden, relationsorden, citationsorden, linjeskift og kanonisk ækvivalente Unicode-former. En ændring i et betydende felt eller en betydende rækkefølge giver et nyt fingerprint, også når skabelonen ikke renderer feltet.

## Afstemning

Afstemningen opretter ét objekt med tre adskilte værdier: aktuelt importeret snapshot, senest godkendt snapshot og dokumentets synlige fakta. Status er:

- `Unchanged`: importeret fingerprint er identisk med det godkendte;
- `Changed`: de komplette fingerprints er forskellige;
- `Missing`: dokumentet har ingen baseline;
- `UnsupportedVersion`: baseline- eller snapshotversionen er ukendt.

Kun `Unchanged` kan være et automatisk no-op. De øvrige tilstande kræver gennemgang eller migrering.
