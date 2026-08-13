# Requirements: Valideret skabelonkontrakt, rendering og medier

- Skabelonens syntaks og alle feltstier skal valideres semantisk før skabelonen kan gemmes eller bruges.
- Ukendte felter, felter i forkert loopkontekst og ugyldige blokke skal give dansk fejl med fil, linje og kolonne.
- Den offentlige feltkontrakt skal være versionsstyret og svare præcist til cheat sheetet.
- Standardskabelonen skal formatere flere forældre med tydelig separator og bruge danske kategorinavne.
- Samme kontekst og skabelon skal give byte-identisk genereret output.
- Relative mediestier skal først opløses i forhold til GEDCOM-kildens mappe og derefter gøres relative til dokumentmappen.
- Mediestier må ikke kunne undslippe tilladte lokale områder uden tydelig advarsel og brugerens valg.
- Manglende eller utilgængelige mediefiler skal give synlig diagnostik uden at stoppe øvrig rendering.
- En ugyldig eller manglende global skabelon må ikke kunne starte en importcommit.
- Skabelonpreview skal bruge samme validering og rendering som den faktiske import.

Den normative kontrakt, standardrendering og lokale mediepolitik er dokumenteret i [Skabelonkontrakt.md](Skabelonkontrakt.md).
