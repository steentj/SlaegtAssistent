# Requirements: Editormodel og generering

- Der skal findes en versioneret, UI-uafhængig editormodel i Core.
- Modellen skal beskrive bloktype, synlighed, rækkefølge, overskrift, valgte felter og understøttet præsentationsform.
- Modellen skal kunne repræsentere hele den indbyggede standardskabelon.
- Personens GEDCOM-notater skal kunne vælges i notatsektionen i skabelonredigeringen og indgå i den genererede biografi.
- Faktablokken skal kunne vises som liste eller tabel. Tabellen skal bevare felternes rækkefølge og udelade manglende oplysninger.
- Familienotater samt hændelsers, kilders og mediers notater skal kunne til- og fravælges i deres respektive blokke og indgå i standardmodellen. Gemte modeller beholder deres eksisterende feltvalg.
- Medienotater vises også uden tilgængelig fil eller valgt titel. Flerlinjede notater skal vises sikkert i lister og tabeller uden at ændre datamodellens linjeskift.
- Modellen skal kunne serialiseres, gemmes og genåbnes med samme indhold.
- Dokumentgenereringen skal være deterministisk og må gerne genbruge den eksisterende renderer internt.
- En ukendt modelversion skal afvises med en tydelig dansk fejl.
- Der stilles ikke krav om indlæsning eller konvertering af det tidligere tekstbaserede format.
- Core må ikke referere til Avalonia eller App-projektet.
