using Avalonia;
using Avalonia.Headless;

namespace SlaegtsAssistent.App.Tests;

public static class HeadlessTestApplication
{
    private static readonly Lazy<HeadlessUnitTestSession> Session = new(
        () => HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication)),
        LazyThreadSafetyMode.ExecutionAndPublication);

    // Kører koden på Avalonias UI-tråd, så vinduer og bindings opfører sig som i den rigtige app.
    public static Task RunAsync(Func<Task> action) => Session.Value.Dispatch(
        async () =>
        {
            await action();
            return true;
        },
        CancellationToken.None);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<SlaegtsAssistent.App.App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
