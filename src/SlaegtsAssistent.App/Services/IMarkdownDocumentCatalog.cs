using System.Collections.Generic;

namespace SlaegtsAssistent.App.Services;

public interface IMarkdownDocumentCatalog
{
    IReadOnlyList<MarkdownDocumentInfo> Load(string? folderPath);
}

public sealed record MarkdownDocumentInfo(
    string RecordId,
    string DisplayName,
    string FilePath,
    string? ErrorMessage = null,
    string? ErrorCategory = null,
    string? NextAction = null,
    bool RequiresMigration = false,
    string? MigrationCandidate = null);
