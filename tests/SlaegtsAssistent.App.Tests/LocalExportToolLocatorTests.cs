using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class LocalExportToolLocatorTests
{
    [Fact]
    public void FinderKunKompletPrivatPakkeOgPrioritererDen()
    {
        var root = Path.Combine(Path.GetTempPath(), "slaegt-vaerktojstest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var bundled = CreatePackage(Path.Combine(root, "tools", "export", "slaegt-export"));
            var development = CreatePackage(Path.Combine(root, "udvikling"));
            LocalExportToolLocator.Find(root, development)!.Executable.Should().Be(bundled);
            File.Delete(Path.Combine(Path.GetDirectoryName(bundled)!, "_internal", "fonts.conf"));
#if DEBUG
            LocalExportToolLocator.Find(root, development)!.Executable.Should().Be(development);
#else
            LocalExportToolLocator.Find(root, development).Should().BeNull();
#endif
            LocalExportToolLocator.Find(root, "slaegt-export").Should().BeNull();
            LocalExportToolLocator.Find(root).Should().BeNull();
        }
        finally { Directory.Delete(root, true); }
    }

    private static string CreatePackage(string directory)
    {
        var internalDirectory = Path.Combine(directory, "_internal");
        Directory.CreateDirectory(Path.Combine(internalDirectory, "fonts"));
        File.WriteAllText(Path.Combine(internalDirectory, "fonts", "graf.ttf"), "skrifttype");
        File.WriteAllText(Path.Combine(internalDirectory, "fonts.conf"), "indhold");
        var executable = Path.Combine(directory, OperatingSystem.IsWindows() ? "slaegt-export.exe" : "slaegt-export");
        File.WriteAllText(executable, "værktøj");
        return executable;
    }
}
