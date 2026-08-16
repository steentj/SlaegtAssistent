# Platform- og distributionstest

## Automatiske porte

Workflowet `.github/workflows/platform-og-distribution.yml` udfører for hver målplatform:

1. arkitekturkontrol;
2. låst restore med .NET SDK 10.0.203;
3. Release-build med warnings som fejl;
4. hele testpakken;
5. Native AOT-publicering;
6. kontrol af hovedartefakten;
7. den indbyggede `--release-smoke-test`;
8. upload af distributionsartefakten.

Målene er macOS ARM64 (`macos-15`/`osx-arm64`), Windows x64 (`windows-latest`/`win-x64`) og Ubuntu x64 (`ubuntu-latest`/`linux-x64`). [GitHubs officielle runneroversigt](https://docs.github.com/en/actions/reference/runners/github-hosted-runners) angiver `macos-15` som ARM64; workflowet kontrollerer desuden `uname -m` og fejler ved forkert arkitektur.

Smoketesten anvender kun en ny midlertidig mappe. Den verificerer GEDCOM-import, Markdown-generering, atomisk settingslagring, snapshot, genindlæsning af dokumentkataloget og privat preview. Den starter ikke et vindue og foretager ingen netværkskald.

## Lokal evidens den 13. august 2026

- Vært: macOS 26.6.1, ARM64.
- SDK: 10.0.203 via `global.json`.
- Almindelig Release-smoketest: bestået.
- Native AOT-publicering for `osx-arm64`: bestået uden AOT- eller trimwarnings efter overgang til source-genereret JSON.
- Resultat: ARM64 Mach-O-hovedprogram og tre private native Avalonia/Skia-biblioteker.
- Kørsel af den lokalt publicerede hovedfil: blokeret af macOS AppleSystemPolicy med exit 137, fordi maskinen ikke har en gyldig Developer ID-identitet (`security find-identity` fandt 0). Det er en distributionssigneringsblokering før applikationsstart, ikke en bestået funktionstest.
- GitHub Actions-matricen er oprettet, men kan først give platformsevidens efter commit/push eller manuel workflowstart.

## Manuel platformstest

Udfør disse trin på hver publiceret platformspakke:

1. Start appen på en maskine uden installeret .NET-runtime.
2. Vælg outputmappe og importér `complete-gedcom-551.ged`.
3. Kontrollér importrapport, danske tegn, kilde, medie, census og ukendt-tag-advarsel.
4. Redigér fri tekst, gem, luk og genåbn. Kontrollér at tekst, dokumentidentitet og rå GEDCOM består.
5. Genimportér uændret fil og kontrollér nul falske ændringer.
6. Importér en ændret fixture, bland feltvalg, afvis andre og genåbn.
7. Åbn fil- og mappedialoger, flyt splitteren og gennemfør primær navigation med tastatur og synlig fokus.
8. Åbn og luk begge hjælpevinduer flere gange uden at blokere hovedvinduet.
9. Kontrollér WebView-preview, lokalt billede og ekstern URL med netværksmonitor.
10. Kontrollér gemmefejl, luk med ugemte ændringer og cancellation under import.
11. Importér den deterministiske 10.000-personers belastningsfixture og observer, at vinduet fortsat kan flyttes, og at cancellation reagerer.

Registrér OS-version, arkitektur, artefaktnavn, commit, resultat og eventuelle afvigelser i `Validation.md`.
