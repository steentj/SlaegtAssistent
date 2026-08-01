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
                var document = BiographyDocumentParser.Parse(File.ReadAllText(filePath));
                if (document.Metadata is { } metadata)
                {
                    documents.Add(new MarkdownDocumentInfo(
                        metadata.RecordId,
                        metadata.DisplayName ?? metadata.RecordId,
                        filePath));
                }
                else
                {
                    documents.Add(new MarkdownDocumentInfo(
                        $"legacy:{Path.GetFileName(filePath)}",
                        Path.GetFileNameWithoutExtension(filePath),
                        filePath));
                }
            }
            catch (IOException exception)
            {
                documents.Add(new MarkdownDocumentInfo(
                    $"error:{Path.GetFileName(filePath)}",
                    Path.GetFileName(filePath),
                    filePath,
                    $"Kunne ikke læse dokumentet: {exception.Message}"));
            }
            catch (FormatException exception)
            {
                documents.Add(new MarkdownDocumentInfo(
                    $"error:{Path.GetFileName(filePath)}",
                    Path.GetFileName(filePath),
                    filePath,
                    $"Dokumentets metadata er ugyldige: {exception.Message}"));
            }
        }

        return documents;
    }
}
