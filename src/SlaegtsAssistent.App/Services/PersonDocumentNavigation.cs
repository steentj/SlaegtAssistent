using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed record PersonDocumentLink(string RecordId, string DisplayName, string FilePath)
{
    public Uri NavigationUri => new("slaegt-person://open/" + Uri.EscapeDataString(RecordId));
}

public static class PersonDocumentNavigation
{
    public static PersonDocumentLink? Resolve(
        string href, string sourcePath, IReadOnlyList<PersonListItemViewModel> people)
    {
        if (string.IsNullOrWhiteSpace(href) || href.StartsWith('#') ||
            Uri.TryCreate(href, UriKind.Absolute, out _) || href.StartsWith('/') || href.StartsWith('\\'))
        {
            return null;
        }

        try
        {
            var relative = Uri.UnescapeDataString(href.Split(['?', '#'], 2)[0]);
            if (Path.IsPathRooted(relative) || relative.Contains(':') || relative.Contains('\\')) return null;
            var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sourcePath))!, relative));
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var matches = people.Where(person => comparer.Equals(Path.GetFullPath(person.MarkdownFilePath), path)).ToArray();
            if (matches.Length != 1) return null;
            var target = matches[0];
            if (target.HasDocumentDiagnostic || target.SyncStatus == BiographySyncStatus.Tvetydig ||
                people.Count(person => person.RecordId == target.RecordId) != 1 || !File.Exists(path)) return null;
            return new PersonDocumentLink(target.RecordId, target.DisplayName, path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or IOException)
        {
            return null;
        }
    }
}
