using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Services;

public sealed class WorkspaceGraphSource
{
    private readonly IMarkdownFileStore _files;
    private readonly MarkdownGraphLinkExtractor _links = new();

    public WorkspaceGraphSource(IMarkdownFileStore files) => _files = files ?? throw new ArgumentNullException(nameof(files));

    public PersonGraph CreateSnapshot(
        IReadOnlyList<PersonListItemViewModel> catalog,
        IReadOnlyDictionary<string, EditorViewModel> editors,
        FamilyTree? tree)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(editors);
        var diagnostics = new List<string>();
        var relations = new List<GraphRelation>();
        var inputs = new List<GraphWorkspacePerson>();
        var grouped = catalog.GroupBy(item => item.RecordId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var ids = grouped.Keys.Concat(tree?.People.Select(person => person.RecordId) ?? [])
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

        foreach (var id in ids)
        {
            var treePerson = tree?.FindPerson(id);
            var fallback = treePerson is null ? null : CanonicalBiographySnapshot.Create(treePerson, tree?.Submitter);
            if (!grouped.TryGetValue(id, out var items))
            {
                inputs.Add(new GraphWorkspacePerson(id, null, null, fallback, false));
                continue;
            }

            if (items.Length != 1 || items[0].HasDocumentDiagnostic || items[0].SyncStatus == BiographySyncStatus.Tvetydig)
            {
                diagnostics.Add($"Dokumentet for '{id}' er ikke entydigt eller har en fejl.");
                inputs.Add(new GraphWorkspacePerson(id, null, null, fallback, treePerson is null));
                continue;
            }

            var item = items[0];
            try
            {
                var document = editors.TryGetValue(item.MarkdownFilePath, out var editor)
                    ? editor.CreateDocument()
                    : BiographyDocumentParser.Parse(_files.Read(item.MarkdownFilePath));
                if (document.Metadata is { } metadata && !string.Equals(metadata.RecordId, id, StringComparison.Ordinal))
                    throw new FormatException("Dokumentets identitet stemmer ikke med kataloget.");
                var baseline = document.Metadata?.SyncBaseline;
                var approved = baseline is { Version: BiographySyncBaseline.CurrentVersion } ? baseline.Approved : null;
                if (baseline is not null && baseline.Version != BiographySyncBaseline.CurrentVersion)
                    diagnostics.Add($"Den godkendte baseline for '{id}' understøttes ikke.");
                inputs.Add(new GraphWorkspacePerson(id, document.Metadata?.Facts, approved, fallback, true));
                relations.AddRange(_links.Extract(id, document.Body,
                    href => PersonDocumentNavigation.Resolve(href, item.MarkdownFilePath, catalog)));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
            {
                diagnostics.Add($"Dokumentet for '{id}' kunne ikke læses: {exception.Message}");
                inputs.Add(new GraphWorkspacePerson(id, null, null, fallback, treePerson is null));
            }
        }

        var graph = new GraphWorkspaceBuilder().Build(inputs, relations);
        return graph with { Diagnostics = Array.AsReadOnly(graph.Diagnostics.Concat(diagnostics).ToArray()) };
    }
}
