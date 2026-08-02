namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyDocumentMetadata(
    int FormatVersion,
    string RecordId,
    string? DisplayName,
    BiographyFactsSnapshot Facts)
{
    public string? GedcomBaselineHash { get; init; }
}
