# Validering: Tegning og interaktion

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Automatisk: verdenspunkt under cursor er invariant ved zoom; screen/world roundtrip og tomme bounds håndteres.
- Headless: klik udsender korrekt id efter zoom; træk og pointer-cancel åbner intet; overlap har stabilt mål.
- Manuel: hover A, H og en node med 100 naboer; alle relevante labels/datoer og oversigt verificeres i begge temaer.
- Manuel/måling: 100/200 % skalering, trackpad/hjul, fokus, minimumsvindue og kontraktens p95-redraw/hovermål.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | G-05, G-06, G-07, G-08, G-12 | Ikke registreret | Afventer implementering | Ingen accept endnu |
| 11. november 2026 | Del af G-06–G-08 | Målrettede `PersonGraphViewModelTests` og `GraphCameraTests` | 4 tests bestået | Nabooplysninger, søgning, probandvalg og kameramatematik er testet isoleret. Tegnekontrol, faktisk pointerinput og visuel validering mangler. |
| 11. november 2026 | Del af G-05–G-08 | `dotnet build --no-restore` og `dotnet test --no-restore` | Build bestået; 146 Core-tests og 220 App-tests bestået | Første tegnekontrol kompilerer, men er ikke koblet til arbejdsbordet og er endnu ikke visuelt eller funktionelt valideret. Overlap, tilgængelighed, ydelse og tematilpasning mangler. |
| 11. november 2026 | Del af G-06–G-08 | `dotnet test --no-restore`; efterfølgende `dotnet build --no-restore` | 147 Core-tests og 222 App-tests bestået; build bestået | Zoomknapper, centrering og rumligt hit-testindeks tilføjet. Første forsøg på overlapplacering og tematilpasset tekst kompilerer; visuel kontrol, høje nabograder og ydelsesmålinger mangler. |
| 11. november 2026 | Del af G-06–G-09 | `dotnet test --no-restore` | 147 Core-tests og 224 App-tests bestået | Headless-test bekræfter hover, direkte naboer, klik og baggrundstræk. Native visuel kontrol mangler; udviklingsvinduet kunne ikke vælges gennem vinduesautomationen. |
