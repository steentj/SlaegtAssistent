# Syntetisk fixture til biografieksport

`BiographyExportIntegrationTests.PakketVaerktoejEksportererBiografierMedTabelBilledeOgLinks` danner syntetiske biografier med danske bogstaver, fri tekst, noter, 60 tabelrækker, billede, manglende billede, kode, et langt navn, en lang URL, links og to personer med samme navn. Testen bruger ingen virkelige personoplysninger. Sæt følgende variabler for at gemme de tre outputfiler til visuel kontrol på macOS:

```bash
SLAEGTSASSISTENT_TEST_EXPORT_BUNDLE="$PWD/tools/export/osx-arm64/slaegt-export/slaegt-export" \
SLAEGTSASSISTENT_TEST_EXPORT_LIBRARIES="$PWD/tools/export/osx-arm64/slaegt-export/_internal" \
SLAEGTSASSISTENT_TEST_EXPORT_FONTS="$PWD/tools/export/osx-arm64/slaegt-export/_internal/fonts.conf" \
SLAEGTSASSISTENT_GOLDEN_OUTPUT=/tmp/slaegt-biografiproever \
dotnet test --no-restore --filter FullyQualifiedName~BiographyExportIntegrationTests
```

Åbn eller konvertér filerne med et faktisk kontorprogram, og render samtlige PDF-sider til billeder. Testens XML- og signaturkontrol er ikke en visuel godkendelse. Den syntetiske fixture er et udgangspunkt; lange navne, lange URL'er, billedtekster og alle aftalte indholdstyper skal fortsat gennemgås i den afsluttende visuelle accept.
