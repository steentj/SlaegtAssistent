namespace SlaegtsAssistent.Core.Domain;

public enum GedcomDiagnosticSeverity
{
    Information,
    Warning,
    Error,
    Fatal,
}

public enum GedcomDiagnosticKind
{
    Other,
    IgnoredMacFamilyTreeMetadata,
    DuplicateMacFamilyTreeMetadata,
}

public sealed record GedcomDiagnostic(
    GedcomDiagnosticSeverity Severity,
    string Message,
    int? Line = null,
    string? RecordId = null,
    string? Tag = null,
    string? Consequence = null,
    string? FilePath = null,
    GedcomDiagnosticKind Kind = GedcomDiagnosticKind.Other);
