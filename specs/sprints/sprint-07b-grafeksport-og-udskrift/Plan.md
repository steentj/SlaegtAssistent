# Plan: Sprint 7B – grafeksport og udskrift

Anden del af roadmap-trin 12. Forudsætning: sprint 7A's grafmodel og koordinater er tilgængelige. Status: planlagt. Leverancen er selvstændig SVG/PNG/PDF samt det eksplicit beskrevne udskriftsflow via systemets PDF-fremviser.

Læs [Eksportkontrakt.md](Eksportkontrakt.md). Implementér:

1. [Lokal værktøjskæde og sidecar](01-lokal-vaerktoejskaede/Plan.md).
2. [Grafeksport og udskriftsflow](02-grafeksport-og-udskriftsflow/Plan.md).

Genbrug procesrunner og værktøjsopslag fra 7A, atomisk filskrivning og eksisterende dialog-/picker-mønstre. Nye App-services: `IExportFilePickerService`, `IGraphExportService`, `GraphExportService`, `ILocalExportSidecar`, `LocalExportSidecar`, `ISystemDocumentLauncher`. Nye ViewModels: `GraphExportViewModel`. Nye Core-typer under `Core/Graph/Export`: `GraphExportOptions`, `GraphExportScene`, `GraphExportSceneBuilder`, `GraphSvgWriter`. Bitmap-rendering forbliver i App, ikke Core.

Første feature skal bevise en rigtig graf-PDF fra en lille fast scene på hver målplatform, før der bygges et fuldt dialogflow. Fastlås herefter værktøjsversioner, licenser og pakningsopsætning. En isoleret prøve er en del af implementeringen; Python- og native-afhængigheder skal ikke vælges frit af kodningsmodellen.

Følg samme arbejdsrytme som [7A](../sprint-07a-personnetvaerk/Plan.md): afgrænset feature, fejlende tests, implementering, dokumenteret manuel kontrol. Godkendelse må ikke foregives ved manglende maskiner eller printer. Ingen GitHub-/cloud-tjeneste kræves under appens brug.
