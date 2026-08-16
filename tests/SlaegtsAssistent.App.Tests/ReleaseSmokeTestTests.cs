using FluentAssertions;

namespace SlaegtsAssistent.App.Tests;

public sealed class ReleaseSmokeTestTests
{
    [Fact]
    public void Run_ShouldVerifyLocalImportDocumentSnapshotAndPreviewFlow()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var exitCode = ReleaseSmokeTest.Run(folder);

            exitCode.Should().Be(0);
            Directory.EnumerateFiles(folder, "*.md").Should().ContainSingle();
            Directory.EnumerateFiles(folder, "manifest.json", SearchOption.AllDirectories)
                .Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
