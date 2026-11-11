using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.Core.Graph;

public sealed record GraphWorkspacePerson(
    string RecordId,
    BiographyFactsSnapshot? DocumentFacts,
    CanonicalBiographySnapshot? Approved,
    CanonicalBiographySnapshot? GedcomFallback,
    bool HasDocument);

public sealed class GraphWorkspaceBuilder
{
    public PersonGraph Build(IReadOnlyList<GraphWorkspacePerson> people, IReadOnlyList<GraphRelation> markdownLinks)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(markdownLinks);
        var diagnostics = new List<string>();
        var nodes = new List<GraphPersonInput>();
        var relations = new List<GraphRelation>(markdownLinks);
        var authoritativeParents = new HashSet<string>(StringComparer.Ordinal);
        var snapshots = new List<CanonicalBiographySnapshot>();

        foreach (var person in people)
        {
            var approved = person.Approved is { Version: CanonicalBiographySnapshot.CurrentVersion } candidate && candidate.Person.RecordId == person.RecordId
                ? candidate : null;
            if (person.Approved is not null && approved is null)
                diagnostics.Add($"Godkendte data for '{person.RecordId}' understøttes ikke.");
            var fallback = !person.HasDocument && person.GedcomFallback is { Version: CanonicalBiographySnapshot.CurrentVersion } gedcom && gedcom.Person.RecordId == person.RecordId
                ? gedcom : null;
            var snapshot = approved ?? fallback;
            if (snapshot is not null) snapshots.Add(snapshot);
            if (person.HasDocument && approved is null)
                diagnostics.Add("Nogle relationer mangler et godkendt datagrundlag.");
            var facts = person.DocumentFacts;
            bool Represents(string name) => facts is not null && (facts.RepresentedFields is null || facts.RepresentedFields.Contains(name));
            nodes.Add(new GraphPersonInput(person.RecordId,
                Represents("Navn") ? facts!.FullName : snapshot?.Person.FullName,
                Represents("Køn") ? facts!.Sex : snapshot?.Person.Sex,
                Represents("Fødselsdato") ? facts!.BirthDate : snapshot?.Person.BirthDate,
                Represents("Dødsdato") ? facts!.DeathDate : snapshot?.Person.DeathDate));

            if (Represents("Forældre"))
            {
                authoritativeParents.Add(person.RecordId);
                foreach (var parent in facts!.ParentRecordIds)
                    relations.Add(new GraphRelation(parent, person.RecordId, GraphRelationKind.Parent));
            }
            else if (snapshot is not null)
            {
                authoritativeParents.Add(person.RecordId);
                foreach (var parent in snapshot.ParentRecordIds)
                    relations.Add(new GraphRelation(parent, person.RecordId, GraphRelationKind.Parent));
            }
        }

        foreach (var snapshot in snapshots)
            foreach (var child in snapshot.ChildRecordIds)
                if (!authoritativeParents.Contains(child))
                    relations.Add(new GraphRelation(snapshot.Person.RecordId, child, GraphRelationKind.Parent));

        foreach (var group in snapshots.SelectMany(snapshot => snapshot.Families).GroupBy(family => family.RecordId, StringComparer.Ordinal))
        {
            var partners = group.Select(family => (family.HusbandRecordId, family.WifeRecordId)).Distinct().ToArray();
            if (partners.Length != 1)
            {
                diagnostics.Add($"Familien '{group.Key}' har modstridende godkendte oplysninger.");
                continue;
            }
            var (first, second) = partners[0];
            if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second))
                relations.Add(new GraphRelation(first, second, GraphRelationKind.Partner, group.Key));
        }

        var graph = new PersonGraphBuilder().Build(nodes, relations);
        return graph with { Diagnostics = Array.AsReadOnly(graph.Diagnostics.Concat(diagnostics).Distinct(StringComparer.Ordinal).ToArray()) };
    }
}
