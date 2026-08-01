using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class MarkdownDocumentCatalogTests
{
    [Fact]
    public void Load_FindsFrontMatterAndLegacyDocuments()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"SlaegtsAssistent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);

        try
        {
            var metadata = new BiographyDocumentMetadata(
                1,
                "@I1@",
                "Anna Jensen",
                new BiographyFactsSnapshot("Anna Jensen", null, null, null, null, null, []));
            File.WriteAllText(
                Path.Combine(folder, "anna.md"),
                BiographyDocumentSerializer.Serialize(metadata, "# Anna Jensen\n"));
            File.WriteAllText(Path.Combine(folder, "legacy.md"), "# Ældre dokument\n");

            var documents = new FileSystemMarkdownDocumentCatalog().Load(folder);

            documents.Should().HaveCount(2);
            documents.Should().Contain(document => document.RecordId == "@I1@");
            documents.Should().Contain(document => document.RecordId == "legacy:legacy.md");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
