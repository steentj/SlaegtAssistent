using System.Collections.Generic;
using System.Linq;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class GedcomDiagnosticViewModel
{
    public GedcomDiagnosticViewModel(GedcomDiagnostic diagnostic, int occurrences = 1)
    {
        Diagnostic = diagnostic;
        Occurrences = occurrences;
    }

    public GedcomDiagnostic Diagnostic { get; }

    public int Occurrences { get; }

    public static IReadOnlyList<GedcomDiagnosticViewModel> CreateSummaries(
        IEnumerable<GedcomDiagnostic> diagnostics)
    {
        var result = new List<GedcomDiagnosticViewModel>();
        var groupIndices = new Dictionary<(GedcomDiagnosticKind Kind, GedcomDiagnosticSeverity Severity,
            string? Tag, string? Consequence, string? FilePath), int>();

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Kind is not (GedcomDiagnosticKind.IgnoredMacFamilyTreeMetadata
                or GedcomDiagnosticKind.DuplicateMacFamilyTreeMetadata))
            {
                result.Add(new GedcomDiagnosticViewModel(diagnostic));
                continue;
            }

            var key = (diagnostic.Kind, diagnostic.Severity, diagnostic.Tag, diagnostic.Consequence, diagnostic.FilePath);
            if (groupIndices.TryGetValue(key, out var index))
            {
                var existing = result[index];
                result[index] = new GedcomDiagnosticViewModel(existing.Diagnostic, existing.Occurrences + 1);
            }
            else
            {
                groupIndices.Add(key, result.Count);
                result.Add(new GedcomDiagnosticViewModel(diagnostic));
            }
        }

        return result;
    }

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
            RecordId,
            Diagnostic.Tag,
            Occurrences > 1 || Diagnostic.Line is null ? null : $"linje {Diagnostic.Line}",
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public string Message
    {
        get
        {
            if (Occurrences == 1)
            {
                return Diagnostic.Message;
            }

            var message = Diagnostic.Kind == GedcomDiagnosticKind.DuplicateMacFamilyTreeMetadata
                ? "Record-id forekommer flere gange i GEDCOM-filen."
                : Diagnostic.Message;
            return $"{message} Antal forekomster: {Occurrences}.";
        }
    }

    public string? Consequence => Diagnostic.Consequence;

    public string? FilePath => Diagnostic.FilePath;

    public string? RecordId => Occurrences > 1 ? null : Diagnostic.RecordId;

    public string DialogText
    {
        get
        {
            var location = Occurrences > 1
                ? Diagnostic.Tag ?? "ukendt tag"
                : $"Linje {Diagnostic.Line?.ToString() ?? "–"}, {Diagnostic.Tag ?? "ukendt tag"}";
            return $"{location}: {Message} Konsekvens: {Consequence}";
        }
    }
}
