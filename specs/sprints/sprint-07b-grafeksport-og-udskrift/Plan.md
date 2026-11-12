# Plan: Sprint 7B – grafeksport og udskrift

Anden del af roadmap-trin 12. **Status: afsluttet for lokal brug på denne Mac**, efter brugerens afgrænsning og manuelle afprøvning. SVG, PNG, PDF og udskriftsflow er implementeret; tekstens læsbarhed følges særskilt i [issue #9](https://github.com/steentj/SlaegtAssistent/issues/9). Den selvstændige publicerede apphost er ikke godkendt på denne Mac; lokal brug er afprøvet via `dotnet`. Valideringsjournalen beskriver de kontroller, der faktisk er gennemført, og de forhold, der ikke er afprøvet.

Læs [Eksportkontrakt.md](Eksportkontrakt.md). Implementér:

1. [Lokal værktøjskæde og sidecar](01-lokal-vaerktoejskaede/Plan.md).
2. [Grafeksport og udskriftsflow](02-grafeksport-og-udskriftsflow/Plan.md).

Genbrug procesrunner og værktøjsopslag fra 7A, atomisk filskrivning og eksisterende dialog-/picker-mønstre. Nye App-services: `IExportFilePickerService`, `IGraphExportService`, `GraphExportService`, `ILocalExportSidecar`, `LocalExportSidecar`, `ISystemDocumentLauncher`. Nye ViewModels: `GraphExportViewModel`. Nye Core-typer under `Core/Graph/Export`: `GraphExportOptions`, `GraphExportScene`, `GraphExportSceneBuilder`, `GraphSvgWriter`. Bitmap-rendering forbliver i App, ikke Core.

For denne lokale leverance fastholdes de afprøvede macOS-arm64-binærpakker i kontrolsummerede arkiver. En ren build genskaber dem uden download. De indsamlede licenser og begrænsninger ved komponenternes oprindelsesoplysninger står i [licensoversigten](../../../vendor/licenser/README.md).

Følg samme arbejdsrytme som [7A](../sprint-07a-personnetvaerk/Plan.md): afgrænset feature, fejlende tests, implementering, dokumenteret manuel kontrol. Godkendelse må ikke foregives ved manglende maskiner eller printer. Ingen GitHub-/cloud-tjeneste kræves under appens brug.
