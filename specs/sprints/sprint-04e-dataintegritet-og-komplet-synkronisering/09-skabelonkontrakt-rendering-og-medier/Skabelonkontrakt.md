# Skabelonkontrakt og mediepolitik

## Versioneret kontrakt

Den offentlige skabelonkontrakt har version 1. `BiographyTemplateContract` er den normative maskinlæsbare liste over rodobjekter, samlinger og felter. Cheat sheetets normative feltliste dannes fra samme kontrakt, så dokumentation og validering ikke kan udvikle sig uafhængigt.

Skabelonloaderen parser først hele skabelonen til en AST og validerer derefter hvert udtryk med den aktuelle løkkekontekst:

- et ukendt rod- eller objektfelt afvises;
- et samlingsfelt skal bruges i en `each`-blok, før elementets felter kan læses;
- et skalart felt kan ikke bruges som løkke;
- et felt fra en anden elementtype, eksempelvis `title` i en hændelsesløkke, afvises;
- syntaksfejl, ugyldige blokke og semantiske fejl indeholder fil, linje og kolonne på dansk.

Samme loader og renderer anvendes ved gemning af indstillinger, skabelonpreview, importens forhåndskontrol og dokumentgenerering. En manglende eller ugyldig global skabelon afbryder derfor før importens commit.

## Standardrendering

Standardskabelonen bruger `person.parentNames`, som formaterer flere forældre med komma og mellemrum. Hændelseskategorier præsenteres på dansk: Fødsel, Dåb, Konfirmation, Vielse, Død, Begravelse, Folketælling, Militærtjeneste og Anden hændelse.

Renderingen er deterministisk: samme kontraktversion, skabelontekst og kontekst giver byte-identisk genereret tekst. Kandidatflowet ændrer fortsat kun metadata og indholdet mellem de genererede markører.

## Lokale medier

En relativ `FILE`-sti opløses først i forhold til GEDCOM-kildens mappe. Den kanoniske filsti omsættes derefter til en URL-kodet, fremad-skråstreget sti relativt til Markdown-dokumentets mappe.

Tilladte områder er GEDCOM-kildens mappe og dokumentets outputmappe. En absolut sti eller en sti med `..`, som ender uden for begge områder, medtages ikke automatisk og giver en synlig fejl, der kræver brugerens valg i den delvise importgennemgang.

En manglende eller ulæselig fil giver en synlig advarsel. Medielinket udelades fra det renderede dokument, men den oprindelige GEDCOM-reference bevares i den kanoniske baseline, og resten af dokumentet renderes normalt.
