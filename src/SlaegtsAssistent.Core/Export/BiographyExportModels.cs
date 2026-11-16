namespace SlaegtsAssistent.Core.Export;

public enum BiographyExportFormat { Docx, Pdf, Odt }
public enum BiographyExportMode { Combined, Separate }

public sealed record BiographyExportItem(string RecordId, string DisplayName, string SourcePath, string Body);

public sealed record BiographyExportRequest(
    IReadOnlyList<BiographyExportItem> Items,
    BiographyExportFormat Format,
    BiographyExportMode Mode);

public sealed record BiographyExportDiagnostic(string RecordId, string DisplayName, string Message);

public sealed record BiographyExportResult(
    IReadOnlyList<string> Saved,
    IReadOnlyList<string> NotSaved,
    IReadOnlyList<BiographyExportDiagnostic> Errors);
