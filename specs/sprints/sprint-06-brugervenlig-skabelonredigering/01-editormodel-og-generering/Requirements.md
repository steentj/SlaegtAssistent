# Requirements: Editormodel og generering

- Der skal findes en versioneret, UI-uafhængig editormodel i Core.
- Modellen skal beskrive bloktype, synlighed, rækkefølge, overskrift, valgte felter og understøttet præsentationsform.
- Modellen skal kunne repræsentere hele den indbyggede standardskabelon.
- Modellen skal kunne serialiseres, gemmes og genåbnes med samme indhold.
- Dokumentgenereringen skal være deterministisk og må gerne genbruge den eksisterende renderer internt.
- En ukendt modelversion skal afvises med en tydelig dansk fejl.
- Der stilles ikke krav om indlæsning eller konvertering af det tidligere tekstbaserede format.
- Core må ikke referere til Avalonia eller App-projektet.
