using System.Linq;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class GedcomDiagnosticViewModel
{
    public GedcomDiagnosticViewModel(GedcomDiagnostic diagnostic)
    {
        Diagnostic = diagnostic;
    }

    public GedcomDiagnostic Diagnostic { get; }

    public GedcomDiagnosticSeverity Severity => Diagnostic.Severity;

    public string SeverityText => Severity switch
    {
        GedcomDiagnosticSeverity.Information => "Information",
        GedcomDiagnosticSeverity.Warning => "Advarsel",
        GedcomDiagnosticSeverity.Error => "Fejl",
        GedcomDiagnosticSeverity.Fatal => "Fatal fejl",
        _ => "Ukendt",
    };

    public string LocationText => string.Join(
        " · ",
        new[]
        {
            Diagnostic.RecordId,
            Diagnostic.Tag,
            Diagnostic.Line is null ? null : $"linje {Diagnostic.Line}",
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public string Message => Diagnostic.Message;

    public string? Consequence => Diagnostic.Consequence;

    public string? FilePath => Diagnostic.FilePath;

    public string? RecordId => Diagnostic.RecordId;
}
