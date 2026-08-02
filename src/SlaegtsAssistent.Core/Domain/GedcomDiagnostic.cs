namespace SlaegtsAssistent.Core.Domain;

public sealed record GedcomDiagnostic(
    string Severity,
    string Message,
    int? Line = null,
    string? RecordId = null,
    string? Tag = null);
