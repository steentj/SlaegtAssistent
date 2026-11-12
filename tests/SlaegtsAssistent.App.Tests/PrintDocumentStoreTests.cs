using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class PrintDocumentStoreTests
{
    [Fact]
    public void GemmerPdfOgFjernerDenFoerstEfterEtDoegn()
    {
        var path = PrintDocumentStore.Save("%PDF-1.7"u8.ToArray());
        var directory = Path.GetDirectoryName(path)!;
        try
        {
            File.Exists(path).Should().BeTrue();
            PrintDocumentStore.Cleanup(Path.GetTempPath(), DateTime.UtcNow);
            File.Exists(path).Should().BeTrue();
            File.SetLastWriteTimeUtc(Path.Combine(directory, ".slaegtsassistent-udskrift"), DateTime.UtcNow.AddDays(-2));
            PrintDocumentStore.Cleanup(Path.GetTempPath(), DateTime.UtcNow);
            Directory.Exists(directory).Should().BeFalse();
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void AfviserUgyldigPdf()
    {
        var action = () => PrintDocumentStore.Save("fejl"u8.ToArray());
        action.Should().Throw<ArgumentException>();
    }
}
