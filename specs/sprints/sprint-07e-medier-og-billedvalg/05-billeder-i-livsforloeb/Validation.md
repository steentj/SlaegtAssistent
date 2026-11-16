# Validering: Billeder ved markøren i livsforløbet

Status: **ikke kørt; featureen er ikke implementeret**.

## Automatiserede prøver

- Indsæt mellem afsnit, i tom biografi, ved slutning, i en tekstlinje og over en fri tekstmarkering; bevar omkringliggende indhold.
- LF/CRLF, æøå, emoji før markøren, alt-tekst med `]`, sti med parenteser, billedtekst med Markdown-tegn.
- Markering delvist inde i generated-/topblok, caret på markørgrænser, ugyldig markørstruktur og dokument uden genererede markører.
- Skift person/arbejdsområde, ændr buffer eller mediemappe under en forsinket fake-vælger: ingen indsættelse i forkert kontekst.
- Flere billeder overlever kandidataccept, afvisning, Gem/genåbn og eksport fra ugemt buffer; native én Fortryd/Gentag genopretter korrekt tekst og caret.

Assertions bruger FluentAssertions. Regressioner skal påvise de konkrete krav i [Requirements.md](Requirements.md), inklusive fejlsituationer; undgå tests, der blot gentager implementationen.

## Manuel prøve

1. Skriv tre afsnit, placér markøren mellem dem og indsæt to forskellige billeder med billedtekst.
2. Fortryd/Gentag, redigér videre og kontrollér preview. Eksportér før Gem og kontrollér, at de nye billeder er med.
3. Prøv indsættelse i fakta og med en markering hen over sektionsgrænsen; teksten skal være uændret.
4. Gem, genstart og gennemfør en kandidatopdatering. Alt frit indhold og billedernes placering skal bestå.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| Ikke kørt | Alle | Afventer implementering | – |
