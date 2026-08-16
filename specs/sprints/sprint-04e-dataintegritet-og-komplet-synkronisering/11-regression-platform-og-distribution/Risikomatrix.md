# Risikomatrix

Matrixen forbinder Missionens garantier og fundene i `Miscellaenous/Review_20260811.md` med automatiseret evidens og den resterende manuelle kontrol. Testnavne er stabile søgenøgler i testprojekterne.

| Garanti eller reviewfund | Positiv automatisk evidens | Fejl- og genstartsevidens | Manuel eller ekstern port |
| --- | --- | --- | --- |
| Fri tekst og eksisterende dokumenter må ikke ødelægges | `Candidate_ShouldRenderSelectedSnapshotAndPreserveFreeTextByteForByte`, `SelectGedcomFileCommand_ShouldApplyApprovedCandidateAndPreserveFreeText` | `WriteText_WhenAWriteStageFails_PreservesExistingFile`, `SelectGedcomFileCommand_WhenCommitFails_ShouldRollbackFilesAndKeepPublishedState`, `ReleaseSmokeTestTests` | Genåbn arbejdsområdet og sammenlign fri tekst byte-for-byte |
| Stabil identitet ved navneændring | `SelectGedcomFileCommand_WhenKnownPersonChangesName_ShouldReuseExistingDocument` | `SelectGedcomFileCommand_WhenRecordIdHasDuplicateDocuments_ShouldMarkAmbiguousAndOpenNeither`, `ReleaseSmokeTestTests` | Kontrollér navneændring og præcis én fil |
| Arbejdsområder må ikke blandes | `SelectGedcomFileCommand_AfterWorkspaceSwitch_ShouldUseOnlyNewWorkspace` | `OpenSettingsCommand_WhenDirtyWorkspaceSwitchIsCancelled_ShouldKeepOldWorkspace`, gem/kassér-varianterne og genindlæsning i `ReleaseSmokeTestTests` | Skift mappe med åbne dokumenter og kontrollér filsystemet |
| Defekte dokumenter må ikke blokere øvrige | `Load_FindsFrontMatterAndLegacyDocuments` | `Load_WhenOneDocumentIsDefective_ContinuesAndReturnsActionableDiagnostic`, dublet- og ukendt-version-tests | Åbn en mappe med defekt og dubleret fil |
| Felt-for-felt-valg og afvisning | `Apply_ShouldCombineIndependentChoicesWithoutChangingInputs`, `SelectGedcomFileCommand_ShouldApplyIndividualFieldChoicesToPreviewCandidate` | `SelectGedcomFileCommand_ShouldLeaveDocumentUnchanged_WhenCandidateIsRejected`, `BulkChoices_ShouldBeReversibleAndUpdatePreviewWithoutApplying` | Bland godkendte og afviste felter og genåbn |
| Komplet GEDCOM 5.5.1-fortolkning | `Load_RepresentativGedcom551FixtureBevarerDenAftalteDatakontrakt`, tegnsæts-, continuation-, citations- og mappingtests | `GedcomFaultToleranceTests`, inklusive lokale recordfejl, dubletter og fatal filstruktur | Importér den repræsentative fixture og gennemgå rapporten |
| Import skal være transaktionel og cancellable | normale import- og partial-import-tests i `MainWindowViewModelTests` | cancellation-, parallel-import-, preflight-, commit-, snapshot- og rollbacktests | Annullér både før review og under et stort datasæt |
| Baseline skal opdage alle understøttede ændringer | `Create_IndeholderAlleUnderstoettedeStruktureredeDatatyper`, `Fingerprint_AendresForAlleHovedtyper` og determinismetest | ukendt baselineversion, manglende baseline og migrationsafvisning i App-tests | Genimportér uændret og derefter ændret GEDCOM |
| Skabelon og medier skal være sikre og deterministiske | `BiographyTemplateContractTests` og golden-master-tests | ukendt felt, forkert loopkontekst, manglende medie og stiudbrud | Skift skabelon og gennemgå kandidat uden automatisk skrivning |
| Preview må ikke kontakte nettet eller køre aktivt indhold | `SafeMarkdownPreviewServiceTests`, `ReleaseSmokeTestTests` | aktive skemaer, ekstern ressource, rå HTML, symbolsk stiudbrud | WebView-test med netværksmonitor på hver platform |
| UI skal kunne indlæses uden rendererfejl og betjenes med tastatur | `AvaloniaHeadlessRegressionTests` for hovedvindue, indstillinger og hjælpevindue | ViewModel-tests for dialogvalg, gemmefejl, lukning og vinduesgenåbning | Tastatur, fokus, splitter, dialoger og vindueslivscyklus på hver platform |
| Store data må ikke fryse UI eller bruge ubegrænset hukommelse | `Load_StorDeterministiskFilBevarerAllePersonerIndenForRessourcerammen` med 10.000 personer og 512 MB allokeringsport | `Load_StorFilRespektererCancellationUdenAtPublicereEtDelvistTrae`; importarbejdet kører via `Task.Run` | Observer UI-respons under import af samme størrelse |
| Reproducerbar cross-platform-distribution | låst SDK, fire `packages.lock.json`, låst restore og publiceringsprofiler | CI afviser build-, test-, publish-, artefakt- eller smoke-fejl | GitHub Actions-matrix og native WebView-smoketest på macOS ARM64, Windows x64 og Linux x64 |

## Repræsentative testdata

- `complete-gedcom-551.ged` dækker GEDCOM 5.5.1-header, UTF-8, submitter, noter med `CONT`/`CONC`, kildecitationer, medie, person- og familiehændelser, census, relationer og et ukendt tag med diagnostik.
- `partial-recovery.ged` og `fatal-missing-trailer.ged` dækker henholdsvis isolerbar og fatal fejl.
- Tegnsætstests genererer UTF-8, ASCII, UTF-16 og ANSEL byte-fixtures eksplicit.
- Den store fixture genereres deterministisk i testen, så repositoryet ikke indeholder en stor redundant fil.
