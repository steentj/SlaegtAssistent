# Validation: Sprint 04E – dataintegritet og komplet synkronisering

## Automatiske kvalitetsporte

- [ ] Alle 11 features har `Requirements.md`, `Plan.md` og `Validation.md`.
- [ ] Alle features har dokumenteret en rød-grøn-refaktorér-cyklus.
- [ ] `dotnet build` består uden warnings.
- [ ] `dotnet test` består samlet.
- [ ] Dataintegritetstests dækker afbrudt skrivning, tilbagerulning og gendannelse.
- [ ] Integrations- og headless UI-tests dækker import, konfliktvalg, fejlvisning og genstart.
- [ ] Platform- og distributionsmatrixen er grøn eller har en udtrykkeligt godkendt, dokumenteret ændring af afgrænsningen.

## Samlet manuel validering

- [ ] Åbn en arbejdsmappe med eksisterende, brugerredigerede dokumenter.
- [ ] Genimportér uændret GEDCOM og bekræft, at der ikke vises falske ændringer.
- [ ] Importér ændret navn, fakta, hændelse, kilde og medie og gennemgå forskellene felt for felt.
- [ ] Afvis udvalgte felter og godkend andre; bekræft at fri tekst er byte-for-byte uændret.
- [ ] Luk og genåbn appen; bekræft dokumentidentitet, status, rå GEDCOM og valgte værdier.
- [ ] Skift arbejdsmappe og bekræft, at den tidligere mappe ikke læses eller skrives efter skiftet.
- [ ] Åbn en mappe med et defekt og et dubleret dokument; bekræft synlige fejl og adgang til øvrige dokumenter.
- [ ] Importér et GEDCOM-testdatasæt med fortsættelseslinjer, citationer, ukendte tags og en lokal postfejl.
- [ ] Bekræft, at preview ikke foretager netværkskald ved eksterne links, billeder eller rå HTML.
- [ ] Gennemfør grundlæggende funktionstest på alle understøttede målplatforme og distributionspakker.

## Endelig godkendelse

- **Dato:**
- **Godkendt af:**
- **Testede platforme:**
- **Build eller commit:**
- **Bemærkninger:**

- [ ] Sprint 04E er manuelt godkendt, og roadmap trin 5 må påbegyndes.
