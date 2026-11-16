using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Services;

public sealed record BiographySnapshotResult(
    IReadOnlyList<BiographyExportItem> Items,
    IReadOnlyList<BiographyExportDiagnostic> Errors);

public sealed class BiographyDocumentSnapshotService(IMarkdownFileStore fileStore)
{
    public BiographySnapshotResult Create(
        IEnumerable<string> selectedIds,
        IEnumerable<MarkdownDocumentInfo> catalog,
        IReadOnlyDictionary<string, EditorViewModel> openEditors)
    {
        ArgumentNullException.ThrowIfNull(selectedIds);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(openEditors);
        var ids = selectedIds.Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length is 0 or > 100)
            return new([], [new("", "", "Vælg mellem 1 og 100 biografier.")]);

        var documents = catalog.ToLookup(item => item.RecordId, StringComparer.Ordinal);
        var items = new List<BiographyExportItem>();
        var errors = new List<BiographyExportDiagnostic>();
        foreach (var id in ids)
        {
            var matches = documents[id].ToArray();
            var name = matches.FirstOrDefault()?.DisplayName ?? id;
            if (matches.Length != 1)
            {
                errors.Add(new(id, name, matches.Length == 0 ? "Biografien findes ikke i dokumentkataloget." : "Flere dokumenter har samme person-id."));
                continue;
            }
            var entry = matches[0];
            if (!string.IsNullOrWhiteSpace(entry.ErrorMessage) || !string.IsNullOrWhiteSpace(entry.ErrorCategory))
            {
                errors.Add(new(id, name, string.IsNullOrWhiteSpace(entry.ErrorMessage) ? entry.ErrorCategory ?? "Dokumentet er fejlbehæftet." : entry.ErrorMessage));
                continue;
            }
            try
            {
                var document = openEditors.TryGetValue(entry.FilePath, out var editor)
                    ? editor.CreateDocument()
                    : BiographyDocumentParser.Parse(fileStore.Read(entry.FilePath));
                if (document.Metadata is not null && !string.Equals(document.Metadata.RecordId, id, StringComparison.Ordinal))
                {
                    errors.Add(new(id, name, "Dokumentets person-id stemmer ikke med kataloget."));
                    continue;
                }
                items.Add(new(id, name, entry.FilePath, document.Body));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                errors.Add(new(id, name, $"Biografien kan ikke læses: {ex.Message}"));
            }
        }
        if (errors.Count != 0) return new([], errors);
        var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), false);
        return new(items.OrderBy(item => item.DisplayName, comparer).ThenBy(item => item.RecordId, StringComparer.Ordinal).ToArray(), []);
    }
}
