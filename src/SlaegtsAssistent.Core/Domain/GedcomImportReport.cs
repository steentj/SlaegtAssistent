namespace SlaegtsAssistent.Core.Domain;

public sealed record GedcomImportReport(
    int ImportedRecords,
    int ImportedWithWarnings,
    int SkippedRecords,
    int FatalErrors,
    IReadOnlyList<GedcomDiagnostic> Diagnostics)
{
    public static GedcomImportReport Empty { get; } = new(0, 0, 0, 0, []);

    public bool IsPartial => SkippedRecords > 0 || Diagnostics.Any(diagnostic =>
        diagnostic.Severity is GedcomDiagnosticSeverity.Error or GedcomDiagnosticSeverity.Fatal);
}
