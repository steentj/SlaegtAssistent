# Plan: Transaktionel og responsiv import

1. Skriv fejlende integrationstests for fejl i hver importfase, annullering og parallel start.
2. Skriv fejlende ViewModel-tests for optaget tilstand, fasevisning og uændret tilstand efter fejl.
3. Udtræk en importkoordinator fra `MainWindowViewModel` med en eksplicit importplan.
4. Implementér sideeffektfri forhåndskontrol og deterministisk gennemgangsmodel.
5. Implementér gennemførelse gennem de atomiske lagringsgrænser fra feature 01.
6. Publicér nyt snapshot, katalog og UI-tilstand samlet efter succesfuld gennemførelse.
7. Flyt tungt arbejde væk fra UI-tråden med videreført annullering.
8. Kør målrettede tests, hele testpakken og build.
9. Gennemfør manuel import af et stort testdatasæt, annullering og kontrolleret fejl i forhåndskontrollen.
10. Dokumentér resultatet, og stop før feature 05.
