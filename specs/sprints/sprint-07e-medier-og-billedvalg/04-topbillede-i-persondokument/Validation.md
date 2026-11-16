# Validering: Ét topbillede pr. persondokument

Status: **delvist implementeret; ikke godkendt**. Tekstoperationer, dialog og editorintegration er prøvet automatisk; manuel validering mangler.

## Automatiserede prøver

- Core: tom body, LF/CRLF, med/uden generated-blok, eksisterende prefix, indsæt/skift/fjern, højst ét billede samt dublerede/ufuldstændige/overlappende markører.
- Tekst før/efter blok er tegn-for-tegn-identisk; metadata, baseline og templatehash er uændrede.
- Editor: ugemt tekst, IsDirty, Gem/genåbn, Annullér, forældet resultat, fil forsvundet og én Fortryd/Gentag.
- Begge opdateringsveje: accepter/afvis ny GEDCOM og ny skabelon, også med nyere topbillede efter kandidatberegning; intet tab eller dubletportræt.

Assertions bruger FluentAssertions. Regressioner skal påvise de konkrete krav i [Requirements.md](Requirements.md), inklusive fejlsituationer; undgå tests, der blot gentager implementationen.

## Manuel prøve

1. Vælg et portræt, ret alternativ tekst og billedtekst, kontrollér placeringen før titlen i preview, Gem og genstart.
2. Skift billedet, Fortryd/Gentag, og fjern det. Kontrollér, at livstekst og originale filer består.
3. Tilføj igen; gennemfør både GEDCOM-opdatering og skabelonopdatering med accept/afvisning. Portrættet skal bestå.
4. Skab en ugyldig topblok manuelt i en syntetisk fil og kontrollér, at kontrollen melder fejl uden at omskrive noget.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| 16. november 2026, macOS | Topblok og kandidatbevaring | Automatiske prøver af indsæt, skift, fjern, ødelagte markører, preview og eksport-HTML | Øvrige kandidatveje og manuel prøve mangler. |

| 16. november 2026, macOS | Valg og fortryd | Headless-prøver for topbillede, fortryd/gentag og ændret buffer under dialog | Native platformprøver og samlet accept mangler. |
