using FluentAssertions;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyDocumentSafeParsingTests
{
    [Fact]
    public void ParseSafely_WhenKeyIsDuplicated_ReturnsPreciseDiagnosticWithoutThrowing()
    {
        const string content = """
            ---
            formatVersion: 1
            recordId: "@I1@"
            recordId: "@I2@"
            facts:
              parentRecordIds: []
            ---
            # Anna
            """;

        var action = () => BiographyDocumentParser.ParseSafely(content);

        action.Should().NotThrow();
        var result = action();
        result.IsSuccess.Should().BeFalse();
        result.Diagnostic!.Category.Should().Be(BiographyDocumentErrorCategory.DuplicateKey);
        result.Diagnostic.Message.Should().Contain("recordId");
        result.Diagnostic.NextAction.Should().Contain("Ret den dublerede nøgle");
    }

    [Theory]
    [InlineData("formatVersion: ugyldig", BiographyDocumentErrorCategory.InvalidValue)]
    [InlineData("formatVersion: 1", BiographyDocumentErrorCategory.MissingRequiredField)]
    public void ParseSafely_WhenRequiredMetadataIsInvalid_ReturnsCategorizedDiagnostic(
        string metadata,
        BiographyDocumentErrorCategory expectedCategory)
    {
        var content = $"---\n{metadata}\nfacts:\n  parentRecordIds: []\n---\n# Anna\n";

        var result = BiographyDocumentParser.ParseSafely(content);

        result.IsSuccess.Should().BeFalse();
        result.Diagnostic!.Category.Should().Be(expectedCategory);
        result.Diagnostic.Message.Should().NotBeNullOrWhiteSpace();
        result.Diagnostic.NextAction.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ParseSafely_WhenFormatVersionIsUnknown_DoesNotReturnDocumentOrMigration()
    {
        const string content = "---\nformatVersion: 99\nrecordId: \"@I1@\"\nfacts:\n  parentRecordIds: []\n---\n# Anna\n";

        var result = BiographyDocumentParser.ParseSafely(content);

        result.IsSuccess.Should().BeFalse();
        result.Document.Should().BeNull();
        result.MigrationCandidate.Should().BeNull();
        result.Diagnostic!.Category.Should().Be(BiographyDocumentErrorCategory.UnsupportedFormatVersion);
        result.Diagnostic.Message.Should().Contain("99");
    }

    [Fact]
    public void ParseSafely_WhenVersionZeroIsSupported_OffersMigrationWithByteIdenticalBody()
    {
        const string body = "# Anna\r\n\r\nFri tekst med æøå.  \r\n";
        var content = "---\r\nformatVersion: 0\r\nrecordId: \"@I1@\"\r\ndisplayName: \"Anna\"\r\nfacts:\r\n  parentRecordIds: []\r\n---\r\n" + body;

        var result = BiographyDocumentParser.ParseSafely(content);

        result.IsSuccess.Should().BeTrue();
        result.RequiresMigration.Should().BeTrue();
        result.Document!.Body.Should().Be(body);
        result.MigrationCandidate.Should().NotBeNull();
        var migrated = BiographyDocumentParser.Parse(result.MigrationCandidate!);
        migrated.Metadata!.FormatVersion.Should().Be(BiographyDocumentParser.CurrentFormatVersion);
        migrated.Body.Should().Be(body);
    }

    [Fact]
    public void ParseSafely_WhenJsonValueIsMalformed_ReturnsInvalidValueDiagnostic()
    {
        const string content = "---\nformatVersion: 1\nrecordId: ikke-json\nfacts:\n  parentRecordIds: []\n---\n# Anna\n";

        var result = BiographyDocumentParser.ParseSafely(content);

        result.IsSuccess.Should().BeFalse();
        result.Diagnostic!.Category.Should().Be(BiographyDocumentErrorCategory.InvalidValue);
        result.Diagnostic.Message.Should().Contain("recordId");
    }
}
