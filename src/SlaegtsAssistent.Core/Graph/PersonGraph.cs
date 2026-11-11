namespace SlaegtsAssistent.Core.Graph;

public sealed record GraphPersonInput(string RecordId, string? Name, string? Sex, string? BirthDate, string? DeathDate);

public enum GraphRelationKind { Parent, Partner, MarkdownLink }

public sealed record GraphRelation(string SourceId, string TargetId, GraphRelationKind Kind, string? FamilyId = null, string? Origin = null);

public sealed record PersonGraphNode(string RecordId, string? Name, string? Sex, string? BirthDate, string? DeathDate);

public sealed record PersonGraphEdge(string FirstId, string SecondId, IReadOnlyList<GraphRelation> Relations);

public sealed record PersonGraph(
    IReadOnlyList<PersonGraphNode> Nodes,
    IReadOnlyList<PersonGraphEdge> Edges,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Neighbours,
    IReadOnlyList<string> Diagnostics);

public sealed class PersonGraphBuilder
{
    public PersonGraph Build(IReadOnlyList<GraphPersonInput> people, IReadOnlyList<GraphRelation> relations)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(relations);

        var diagnostics = new List<string>();
        var nodes = new Dictionary<string, PersonGraphNode>(StringComparer.Ordinal);
        foreach (var person in people)
        {
            if (string.IsNullOrWhiteSpace(person.RecordId))
            {
                diagnostics.Add("En person mangler record-id.");
                continue;
            }

            var node = new PersonGraphNode(person.RecordId, person.Name, person.Sex, person.BirthDate, person.DeathDate);
            if (!nodes.TryAdd(person.RecordId, node))
            {
                diagnostics.Add($"Record-id '{person.RecordId}' findes flere gange.");
                nodes[person.RecordId] = new PersonGraphNode(person.RecordId, null, null, null, null);
            }
        }

        var edges = new Dictionary<(string First, string Second), HashSet<GraphRelation>>();
        foreach (var relation in relations)
        {
            if (!nodes.ContainsKey(relation.SourceId) || !nodes.ContainsKey(relation.TargetId))
            {
                diagnostics.Add("En forbindelse henviser til en ukendt person.");
                continue;
            }
            if (string.Equals(relation.SourceId, relation.TargetId, StringComparison.Ordinal))
            {
                diagnostics.Add($"Personen '{relation.SourceId}' har en forbindelse til sig selv.");
                continue;
            }
            var pair = StringComparer.Ordinal.Compare(relation.SourceId, relation.TargetId) < 0
                ? (relation.SourceId, relation.TargetId) : (relation.TargetId, relation.SourceId);
            if (!edges.TryGetValue(pair, out var causes)) edges.Add(pair, causes = []);
            causes.Add(relation);
        }

        var resultEdges = edges.OrderBy(e => e.Key.First, StringComparer.Ordinal)
            .ThenBy(e => e.Key.Second, StringComparer.Ordinal)
            .Select(e => new PersonGraphEdge(e.Key.First, e.Key.Second, Array.AsReadOnly(e.Value
                .OrderBy(r => r.Kind).ThenBy(r => r.SourceId, StringComparer.Ordinal)
                .ThenBy(r => r.TargetId, StringComparer.Ordinal).ThenBy(r => r.FamilyId, StringComparer.Ordinal)
                .ThenBy(r => r.Origin, StringComparer.Ordinal).ToArray())))
            .ToArray();
        var neighbours = nodes.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in resultEdges)
        {
            neighbours[edge.FirstId].Add(edge.SecondId);
            neighbours[edge.SecondId].Add(edge.FirstId);
        }
        var readonlyNeighbours = neighbours.ToDictionary(entry => entry.Key,
            entry => (IReadOnlyList<string>)Array.AsReadOnly(entry.Value.Order(StringComparer.Ordinal).ToArray()), StringComparer.Ordinal);
        return new PersonGraph(
            Array.AsReadOnly(nodes.Values.OrderBy(node => node.RecordId, StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(resultEdges),
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyList<string>>(readonlyNeighbours),
            diagnostics.AsReadOnly());
    }
}
