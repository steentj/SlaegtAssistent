namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyDocumentParseResult(
    BiographyDocument? Document,
    BiographyDocumentDiagnostic? Diagnostic,
    bool RequiresMigration = false,
    string? MigrationCandidate = null)
{
    public bool IsSuccess => Document is not null && Diagnostic is null;
}
