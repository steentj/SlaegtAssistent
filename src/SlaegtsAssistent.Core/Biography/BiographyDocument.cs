namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyDocument(
    BiographyDocumentMetadata? Metadata,
    string Body,
    bool HasFrontMatter);
