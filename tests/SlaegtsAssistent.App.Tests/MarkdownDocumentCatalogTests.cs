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

    [Fact]
    public void Load_WhenOneDocumentIsDefective_ContinuesAndReturnsActionableDiagnostic()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"SlaegtsAssistent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var validPath = Path.Combine(folder, "gyldig.md");
        var defectivePath = Path.Combine(folder, "defekt.md");

        try
        {
            var metadata = new BiographyDocumentMetadata(
                1,
                "@I1@",
                "Anna Jensen",
                new BiographyFactsSnapshot("Anna Jensen", null, null, null, null, null, []));
            File.WriteAllText(validPath, BiographyDocumentSerializer.Serialize(metadata, "# Anna\n"));
            File.WriteAllText(
                defectivePath,
                "---\nformatVersion: 1\nrecordId: \"@I2@\"\nrecordId: \"@I3@\"\n---\n# Defekt\n");

            var documents = new FileSystemMarkdownDocumentCatalog().Load(folder);

            documents.Should().HaveCount(2);
            documents.Should().Contain(document => document.RecordId == "@I1@");
            var defective = documents.Single(document => document.FilePath == defectivePath);
            defective.RecordId.Should().StartWith("error:");
            defective.ErrorCategory.Should().Be("Dubleret nøgle");
            defective.ErrorMessage.Should().Contain("recordId");
            defective.NextAction.Should().Contain("Ret den dublerede nøgle");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenVersionIsUnknown_DoesNotExposeItAsKnownRecordId()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"SlaegtsAssistent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "ukendt-version.md");

        try
        {
            File.WriteAllText(
                path,
                "---\nformatVersion: 99\nrecordId: \"@I1@\"\nfacts:\n  parentRecordIds: []\n---\n# Anna\n");

            var document = new FileSystemMarkdownDocumentCatalog().Load(folder).Single();

            document.RecordId.Should().StartWith("error:");
            document.ErrorCategory.Should().Be("Ikke-understøttet formatversion");
            document.MigrationCandidate.Should().BeNull();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenVersionZeroIsSupported_ExposesMigrationCandidateWithoutChangingFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"SlaegtsAssistent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "version-0.md");
        const string content = "---\nformatVersion: 0\nrecordId: \"@I1@\"\ndisplayName: \"Anna\"\nfacts:\n  parentRecordIds: []\n---\n# Anna\n\nFri tekst.\n";

        try
        {
            File.WriteAllText(path, content);

            var document = new FileSystemMarkdownDocumentCatalog().Load(folder).Single();

            document.RecordId.Should().Be("@I1@");
            document.RequiresMigration.Should().BeTrue();
            document.MigrationCandidate.Should().NotBeNull();
            File.ReadAllText(path).Should().Be(content);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenRecordIdIsDuplicated_MarksEveryMatchingDocumentAsAmbiguous()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"SlaegtsAssistent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var metadata = new BiographyDocumentMetadata(
            1,
            "@I1@",
            "Anna",
            new BiographyFactsSnapshot("Anna", null, null, null, null, null, []));

        try
        {
            File.WriteAllText(
                Path.Combine(folder, "anna-a.md"),
                BiographyDocumentSerializer.Serialize(metadata, "# Første\n"));
            File.WriteAllText(
                Path.Combine(folder, "anna-b.md"),
                BiographyDocumentSerializer.Serialize(metadata, "# Andet\n"));

            var documents = new FileSystemMarkdownDocumentCatalog().Load(folder);

            documents.Should().HaveCount(2);
            documents.Should().OnlyContain(document => document.RecordId == "@I1@");
            documents.Should().OnlyContain(document => document.ErrorCategory == "Tvetydigt record-id");
            documents.Should().OnlyContain(document => document.NextAction!.Contains("Sammenlign filerne"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
