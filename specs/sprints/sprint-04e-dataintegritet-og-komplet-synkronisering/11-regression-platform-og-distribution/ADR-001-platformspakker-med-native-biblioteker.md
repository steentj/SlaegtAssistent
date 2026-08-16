# ADR-001: Platformspakker med private native biblioteker

- **Status:** Godkendt
- **Dato:** 13. august 2026
- **Godkendt af:** Produktejer den 13. august 2026

## Kontekst

`TechStack.md` kræver Native AOT og én enkelt eksekverbar fil pr. platform. Native AOT-hovedprogrammet kan bygges, men Avalonia/Skia leverer native platformskomponenter (`libAvaloniaNative`, `libHarfBuzzSharp` og `libSkiaSharp` på macOS), som fortsat udgives ved siden af hovedprogrammet. På macOS kræver en distribuerbar pakke desuden Developer ID-signering og notarisering; den lokale udviklingsmaskine har ingen gyldig signeringsidentitet.

At kalde dette en enkeltfil uden disse afhængigheder ville være misvisende, og at indlejre dem som runtime-self-extract ændrer ikke kravet om signering og native loading.

## Beslutning

- Bevar Native AOT og self-contained distribution uden krav om installeret .NET-runtime.
- Distribuér som én platformspakke frem for bogstaveligt én fil:
  - signeret og notariseret `.app`/`.dmg` på macOS;
  - signeret mappebaseret pakke eller installer på Windows;
  - AppImage eller arkiv med private biblioteker på Linux.
- Hold alle native afhængigheder private inde i platformspakken.
- Kræv build, automatiseret CLI-smoketest og manuel native WebView-smoketest af den færdige pakke på hver platform.
- Publicér ikke en macOS-udgivelse uden Developer ID-signering og notarisering.

## Konsekvenser

Brugeren modtager fortsat én installations- eller applikationspakke og behøver ingen .NET-runtime. Den bogstavelige enkeltfilsgaranti ændres, men platformenes normale sikkerheds- og bundlingsmodel respekteres. CI kan bevise kompilering og intern funktion; release-signering kræver hemmelige certifikater i et beskyttet releaseflow.

## Alternativer

- Udskifte Avalonia/Skia/WebView med en stack uden native biblioteker: uforholdsmæssig arkitekturændring og tab af roadmaparbejde.
- Udlevere usignerede løse filer: afvist på grund af macOS-sikkerhed og dårlig brugeroplevelse.
- Hævde enkeltfil ved selvudpakning: afvist, fordi native filer stadig eksisterer ved kørsel og skal sikkerhedshåndteres.
