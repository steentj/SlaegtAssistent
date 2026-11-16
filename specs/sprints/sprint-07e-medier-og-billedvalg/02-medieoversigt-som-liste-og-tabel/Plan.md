# Plan: Medieoversigt uden automatiske billeder

Status: **planlagt**. Estimat: **1–2 udviklingsdage**. Afhænger af feature: 01.

Gør den genererede medieblok til en oversigt i liste- eller tabelform. Manuel billedplacering forbliver uafhængig af skabelonen.

Kontrakter: [Mediekontrakt.md](../Mediekontrakt.md). Krav: [Requirements.md](Requirements.md). Validering: [Validation.md](Validation.md).

## Implementeringstrin

1. Skriv regression, der viser den nuværende automatiske billedsyntaks i `AppendMedia`, og specificér det ønskede tekstresultat.
2. Udvid `BiographyTemplateEditorCatalog.GetPresentations` til Liste/Tabel for Medier; ret hjælpetekster og `BiographyTemplateEditorModel.CreateDefault` til Liste.
3. Implementér liste og tabel i `BiographyTemplateEditorCompiler`. Respekter valgte felter og rækkefølge. Lokal fil bliver tekst, ikke et billede eller et automatisk åbnet fillink. Manglende medier forbliver repræsenteret med forståelig status.
4. Accepter tidligere Standard som liste ved kompilering/validering. Vis Liste ved åbning i editoren og gem kun den normaliserede model, når brugeren gemmer. Indlæste skabeloner skal ikke overskrives automatisk.
5. Tilpas eksempeldata, skabelonhjælp og forventede testresultater. Kontroller rå skabelonkontrakt og mediefelter: der indføres ingen automatisk billedudvidelse. Håndskrevet Markdown, inklusive manuelt valgte billeder, bevares.
6. Lad den eksisterende kandidatvej tilbyde ændring af allerede genererede afsnit. Afvisning efterlader dokumenterne uændrede; accept ændrer kun genereret område. Ret den relevante dokumentation i Mission.md, så automatisk medieoversigt og manuelt indlagte billeder beskrives præcist.

## Afslutningskriterium

Alle featurekrav er dækket af relevante tests, det manuelle flow er gennemført, og resultaterne er skrevet i Validation.md. Fejlende tests skrives før adfærdsændringer i stiopslag, tekstoperationer og kandidatbevaring. Featureen må ikke erklæres færdig alene på baggrund af UI-skærmbilleder.
