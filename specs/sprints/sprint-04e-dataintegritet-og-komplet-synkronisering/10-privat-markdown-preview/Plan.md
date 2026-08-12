# Plan: Privat og kontrolleret Markdown-preview

1. Skriv fejlende sikkerhedstests for eksterne billeder, scripts, iframe, stylesheets, skrifttyper og URL-skemaer.
2. Skriv fejlende tests for tilladte lokale billeder, blokerede stiudbrud og linkbekræftelse.
3. Definér en fælles previewpolitik og HTML-rensning uden ViewModel-afhængighed af Avalonia.
4. Implementér sikker Markdig-pipeline, renser, sikkerhedspolitik for indhold og lokal ressourceopløser.
5. Integrér WebView-navigation og linkhåndtering med deny-by-default.
6. Genbrug samme pipeline i hovedpreview og hjælpevinduer.
7. Tilføj en testserver eller netværksobservatør, der beviser nul udgående anmodninger.
8. Kør målrettede tests, hele testpakken og build.
9. Gennemfør manuel previewtest med netværksmonitor i alle temaer.
10. Dokumentér resultatet, og stop før feature 11.
