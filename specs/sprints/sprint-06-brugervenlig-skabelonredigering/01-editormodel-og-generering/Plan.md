# Plan: Editormodel og generering

1. Kortlæg standardskabelonen og definér det mindste sæt bloktyper og indstillinger, som dækker den fuldt ud.
2. Beskriv modelversion og kanonisk serialiseringsformat i [Modelkontrakt.md](Modelkontrakt.md); metadata må ikke ændre det renderede dokument.
3. Skriv fejlende Core-tests for standardmodel, blokorden, skjulte blokke, liste/tabel, serialisering, genåbning og determinisme.
4. Implementér immutable modeller samt et katalog med danske labels adskilt fra tekniske feltstier.
5. Implementér deterministisk dokumentgenerering; genbrug eventuelt den eksisterende DSL og renderer som intern detalje.
6. Implementér indlæsning af den nye model og en tydelig dansk fejl ved ukendt version.
7. Test standardskabelonen byte-for-byte eller dokumentér og godkend en tilsigtet kanonisk ændring.
8. Kør Core-tests og stop for manuel gennemgang af den genererede standardtekst.
