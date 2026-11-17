# Modelkontrakt: brugervenlig persondokumentskabelon

## Format og version

Nye skabeloner gemmes som UTF-8 uden byte order mark med filendelsen `.slaegtskabelon`. Indholdet er indrykket JSON med denne overordnede form:

```json
{
  "version": 1,
  "blocks": []
}
```

`version` er obligatorisk. Kun version 1 understøttes i dette sprint. En ukendt version afvises med en dansk fejl og fortolkes ikke som det tidligere tekstbaserede skabelonformat.

Blokkenes rækkefølge i `blocks` er dokumentets rækkefølge. Hver blok indeholder:

- `type`: blokkens stabile tekniske type;
- `isVisible`: om blokken skal genereres;
- `heading`: den danske overskrift, hvor bloktypen bruger en overskrift;
- `presentation`: `Standard`, `List` eller `Table`;
- `fields`: valgte felter i den rækkefølge, de skal præsenteres.

Serialiseringen er kanonisk: samme model giver byte-identisk JSON med afsluttende linjeskift.

## Bloktyper

| Type | Dansk navn | Visning |
| --- | --- | --- |
| `Title` | Titel | Standard |
| `Facts` | Fakta | Liste eller tabel |
| `Events` | Hændelser | Liste eller tabel |
| `Census` | Folketællinger | Liste eller tabel |
| `Sources` | Kilder | Liste eller tabel |
| `Media` | Medier | Liste eller tabel (`Standard` i ældre modeller understøttes) |
| `Submitter` | Afsender | Standard |
| `Biography` | Biografi | Standard |

En bloktype må højst forekomme én gang. Skjulte blokke bevares i modellen, men genererer intet dokumentindhold.

## Feltgrupper

- Titel: personens navn.
- Fakta: fødsel, død, forældre, børn, partnere, oprettelsesdato, notat og familienotat.
- Hændelser: kategori, type, oplysning, dato, sted og notat.
- Folketællinger: dato, sted og notat.
- Kilder: titel, forfatter, side og notat.
- Medier: titel, lokal fil og notat.
- Afsender: navn.
- Biografi: ingen strukturerede felter; blokken reserverer plads til brugerens frie tekst.

`BiographyTemplateEditorCatalog` er den fælles kilde til danske blok- og feltnavne. Tekniske feltstier holdes adskilt fra de tekster, som senere vises i brugergrænsefladen.

## Dokumentgenerering

`BiographyTemplateEditorCompiler` omsætter modellen deterministisk til den eksisterende sikre renderingsmekanisme. Standardmodellen omfatter alle notatfelterne; tidligere gemte modeller ændres ikke automatisk, men felterne kan tilvælges. Medienotater vises uafhængigt af filens tilgængelighed og titelens tilvalg. I tabeller vises linjeskift som mellemrum, uden at ændre de lagrede data. Tidligere brugerdefinerede tekstskabeloner er ikke en del af denne kontrakt.

Generatoren afviser blandt andet:

- ukendt modelversion;
- dublerede bloktyper eller felter;
- felter i en forkert blok;
- en visningsform, som bloktypen ikke understøtter;
- påkrævede tomme overskrifter eller overskrifter med linjeskift.
