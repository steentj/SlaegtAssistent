# Plan: Guidet blokeditor

1. Skitsér master/detail-flowet med blokliste, konfiguration og tydelige primære handlinger.
2. Skriv fejlende ViewModel-tests for valg, tilføjelse, synlighed, flytning, feltorden, præsentationsform, fortryd/gendan og nulstilling.
3. Implementér en editor-ViewModel, som kun arbejder på Core-editormodellen og abstraherede dialogservices.
4. Byg Avalonia-visningen med responsive paneler, dansk tekst, synlige fokus-states og tilgængelige navne.
5. Vis basale valg først og placér sjældne indstillinger under “Flere muligheder”.
6. Deaktivér eller skjul irrelevante valg med en forklaring, der kan forstås uden kendskab til rendererens kontrakt.
7. Tilføj tastaturkommandoer for bloknavigation og flytning uden at gøre genveje til eneste adgang.
8. Kør App-tests og gennemfør manuel redigering ved minimumsstørrelse og 200 % skalering.
