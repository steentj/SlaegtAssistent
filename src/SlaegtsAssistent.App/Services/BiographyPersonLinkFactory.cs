using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public static class BiographyPersonLinkFactory
{
    public static BiographyPersonLinks Create(
        FamilyTree tree,
        string outputDirectory,
        IReadOnlyList<MarkdownDocumentInfo> documents)
    {
        var paths = new List<KeyValuePair<string, string>>();
        var planned = tree.People.ToDictionary(person => person.RecordId,
            person => Path.Combine(outputDirectory, BiographyFileNameGenerator.Generate(person)), StringComparer.Ordinal);
        var byId = documents.ToLookup(document => document.RecordId, StringComparer.Ordinal);
        var byPath = documents.ToLookup(document => document.FilePath, StringComparer.Ordinal);
        var plannedCounts = planned.Values.GroupBy(path => path, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (var person in tree.People)
        {
            var matches = byId[person.RecordId].ToArray();
            if (matches.Length == 1 && string.IsNullOrWhiteSpace(matches[0].ErrorCategory))
            {
                paths.Add(new(person.RecordId, matches[0].FilePath));
            }
            else if (matches.Length == 0)
            {
                var path = planned[person.RecordId];
                var atPath = byPath[path].ToArray();
                if ((atPath.Length == 1 && atPath[0].RecordId.StartsWith("legacy:", StringComparison.Ordinal)) ||
                    (atPath.Length == 0 && !File.Exists(path) && plannedCounts[path] == 1))
                {
                    paths.Add(new(person.RecordId, path));
                }
            }
        }

        return new BiographyPersonLinks(paths);
    }
}
