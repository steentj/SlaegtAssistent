# Validation: Samlet regression, platform og distribution

- [x] Risikomatricen dækker alle Mission-garantier og reviewfund.
- [x] Atomisk skrivning, identitet, workspace, importtransaktion og restart har integrationstests.
- [x] Kritiske dialog-, binding-, tastatur- og vinduesforløb har rendererløse UI-tests; native adapteradfærd indgår i platformstesten.
- [x] GEDCOM-testdatapakken dækker alle aftalte mappings og fejltilstande.
- [ ] Core-ressourceforbrug er verificeret deterministisk med 10.000 personer og 512 MB-port; faktisk UI-respons uden frysning afventer manuel platformstest.
- [x] SDK og NuGet-resolution er reproducerbart låst.
- [ ] CI er grøn på macOS ARM64, Windows x64 og understøttet Linux.
- [x] Native AOT er kompileret uden AOT-/trimwarnings, og den ændrede distributionskontrakt er godkendt i `ADR-001`.
- [ ] WebView, filvalg, import, gemning og lukning har gennemgået grundlæggende funktionstest på hver platform.
- [x] Alle featuregodkendelser fra 01–10 er udfyldt.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Samlet manuel test af en genåbnet arbejdsmappe er gennemført.

## Automatiseret evidens

- Rendererfrie Avalonia-regressioner: 4 bestået, 0 fejlet.
- Deterministiske store-data-regressioner: 2 bestået, 0 fejlet.
- Release-smoketest gennem almindeligt Release-build: bestået.
- macOS ARM64 Native AOT-publicering: kompileret uden AOT- eller trimwarnings.
- Platformsmatrix, låst restore, publicering, artefaktkontrol og CLI-smoketest er implementeret i GitHub Actions.
- Release-build med warnings som fejl: 0 warnings og 0 fejl.
- Fuld Release-testpakke den 13. august 2026: 108 Core-tests og 120 App-tests bestået, 0 fejlet.

## Rød-grøn-refaktorér

1. Rendererfrie vindues-, binding- og tastaturtests blev skrevet først. Den første kørsel afslørede både en inkompatibel xUnit-adapter og manglende logisk træinitialisering; testen blev grøn med Avalonias rendererfrie platform på den eksisterende xUnit-version.
2. Release-smoketesten blev skrevet før `ReleaseSmokeTest` og fejlede ved kompilering. Implementeringen gjorde import-, dokument-, snapshot-, genstarts- og previewkæden grøn.
3. Native AOT-kørslen producerede trim- og dynamisk-JSON-warnings. JSON blev refaktoreret til source-genererede kontekster, og en ny publicering blev warning-fri uden ændring af serialiseringskontrakten; hele regressionspakken blev derefter kørt igen.
4. Den deterministiske 10.000-personers ressource- og cancellationtest blev tilføjet som en permanent port uden vilkårlige tidsgrænser.

## Åbne manuelle og eksterne porte

- Workflowet skal køres og være grønt på de tre målplatforme.
- Native UI, WebView, filvælgere og tastatur skal kontrolleres efter `Platformtest.md`.
- macOS-artefakten skal signeres og notariseres med en gyldig Developer ID-identitet før udgivelsens smoketest.
- `ADR-001` blev godkendt af produktejeren den 13. august 2026; én signeret platformspakke erstatter kravet om bogstaveligt én fil.

## Manuel godkendelse

- **Dato:**
- **Godkendt af:**
- **Build eller commit:**
- **Testede platforme:**
- **Bemærkninger:**

- [ ] Feature 11 og Sprint 04E er godkendt, og roadmap trin 5 må påbegyndes.
