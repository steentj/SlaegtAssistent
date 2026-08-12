using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed class FileSystemMarkdownDocumentCatalog : IMarkdownDocumentCatalog
{
    public IReadOnlyList<MarkdownDocumentInfo> Load(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return [];
        }

        var documents = new List<MarkdownDocumentInfo>();
        foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.md", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase))
        {
            try
            {
                var result = BiographyDocumentParser.ParseSafely(File.ReadAllText(filePath));
                if (!result.IsSuccess)
                {
                    var diagnostic = result.Diagnostic!;
                    documents.Add(new MarkdownDocumentInfo(
                        $"error:{Path.GetFileName(filePath)}",
                        Path.GetFileName(filePath),
                        filePath,
                        diagnostic.Message,
                        ToDanishCategory(diagnostic.Category),
                        diagnostic.NextAction));
                }
                else if (result.Document!.Metadata is { } metadata)
                {
                    documents.Add(new MarkdownDocumentInfo(
                        metadata.RecordId,
                        metadata.DisplayName ?? metadata.RecordId,
                        filePath,
                        result.RequiresMigration
                            ? "Dokumentet bruger en ældre, understøttet formatversion."
                            : null,
                        null,
                        result.RequiresMigration
                            ? "Gennemse og godkend migrationsforslaget, før filen ændres."
                            : null,
                        result.RequiresMigration,
                        result.MigrationCandidate));
                }
                else
                {
                    documents.Add(new MarkdownDocumentInfo(
                        $"legacy:{Path.GetFileName(filePath)}",
                        Path.GetFileNameWithoutExtension(filePath),
                        filePath));
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                documents.Add(new MarkdownDocumentInfo(
                    $"error:{Path.GetFileName(filePath)}",
                    Path.GetFileName(filePath),
                    filePath,
                    $"Der mangler adgang til dokumentet: {exception.Message}",
                    "Adgangsfejl",
                    "Kontrollér filens adgangsrettigheder, og indlæs arbejdsområdet igen."));
            }
            catch (IOException exception)
            {
                documents.Add(new MarkdownDocumentInfo(
                    $"error:{Path.GetFileName(filePath)}",
                    Path.GetFileName(filePath),
                    filePath,
                    $"Kunne ikke læse dokumentet: {exception.Message}",
                    "Læsefejl",
                    "Kontrollér filens adgangsrettigheder, og indlæs arbejdsområdet igen."));
            }
        }

        return MarkDuplicateRecordIds(documents);
    }

    private static IReadOnlyList<MarkdownDocumentInfo> MarkDuplicateRecordIds(
        IReadOnlyList<MarkdownDocumentInfo> documents)
    {
        var duplicateIds = documents
            .Where(document => !document.RecordId.StartsWith("error:", StringComparison.Ordinal) &&
                               !document.RecordId.StartsWith("legacy:", StringComparison.Ordinal))
            .GroupBy(document => document.RecordId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        return documents.Select(document => duplicateIds.Contains(document.RecordId)
            ? document with
            {
                ErrorMessage = $"Record-id '{document.RecordId}' findes i flere Markdown-dokumenter.",
                ErrorCategory = "Tvetydigt record-id",
                NextAction = "Sammenlign filerne manuelt, og behold eller ret kun den tilsigtede fil.",
            }
            : document).ToList();
    }

    private static string ToDanishCategory(BiographyDocumentErrorCategory category)
    {
        return category switch
        {
            BiographyDocumentErrorCategory.MalformedFrontMatter => "Ugyldig frontmatter",
            BiographyDocumentErrorCategory.DuplicateKey => "Dubleret nøgle",
            BiographyDocumentErrorCategory.InvalidValue => "Ugyldig værdi",
            BiographyDocumentErrorCategory.MissingRequiredField => "Manglende obligatorisk felt",
            BiographyDocumentErrorCategory.UnsupportedFormatVersion => "Ikke-understøttet formatversion",
            _ => "Dokumentfejl",
        };
    }
}
