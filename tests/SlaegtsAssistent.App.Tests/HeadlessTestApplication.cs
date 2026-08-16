using Avalonia;
using Avalonia.Headless;

namespace SlaegtsAssistent.App.Tests;

public static class HeadlessTestApplication
{
    private static readonly object SyncRoot = new();

    public static void EnsureInitialized()
    {
        if (Application.Current is not null)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (Application.Current is null)
            {
                BuildAvaloniaApp().SetupWithoutStarting();
            }
        }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<SlaegtsAssistent.App.App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
