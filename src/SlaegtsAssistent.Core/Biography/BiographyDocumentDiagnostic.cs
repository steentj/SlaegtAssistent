namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyDocumentDiagnostic(
    BiographyDocumentErrorCategory Category,
    string Message,
    string NextAction);
