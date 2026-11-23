using System.Globalization;
using System.Numerics;

namespace SlaegtsAssistent.Core.FamilyTrees;

public enum FamilyTreeExportLayout { Detailed, Compact, Vertical }

public sealed record FamilyTreeExportCell(
    FamilyTreePerson? Person, IReadOnlyList<BigInteger> AncestorNumbers)
{
    public IReadOnlyList<string> Format(FamilyTreeExportLayout layout)
    {
        var number = AncestorNumbers.Count == 0
            ? "#Ukendt"
            : string.Join(", ", AncestorNumbers.Select(value => "#" + value.ToString(CultureInfo.InvariantCulture)));
        var name = Person?.Name ?? "Ukendt";
        var heading = $"{number} {name}";
        if (layout != FamilyTreeExportLayout.Detailed)
        {
            var birth = FamilyTreeIndex.FormatYear(Person?.BirthDate);
            var death = FamilyTreeIndex.FormatYear(Person?.DeathDate);
            return [heading, $"{birth} – {death}"];
        }

        return [heading,
            $"Født: {Person?.BirthDate ?? "Ukendt"}",
            $"Fødested: {Person?.BirthPlace ?? "Ukendt"}",
            $"Død: {Person?.DeathDate ?? "Ukendt"}",
            $"Dødssted: {Person?.DeathPlace ?? "Ukendt"}"];
    }
}

public sealed record FamilyTreeExportRow(
    int Generation, string Label, IReadOnlyList<FamilyTreeExportCell> Cells)
{
    public int? GridSlots { get; init; }
    public IReadOnlyList<int>? GridPositions { get; init; }
    public FamilyTreeExportLayout Layout => Cells.Count <= 5 ? FamilyTreeExportLayout.Detailed
        : Cells.Count <= 8 ? FamilyTreeExportLayout.Compact : FamilyTreeExportLayout.Vertical;
}

public sealed record FamilyTreeExportPlan(
    string MainProbandId, string OriginalAncestorNumber, FamilyTreePerson Root,
    IReadOnlyList<FamilyTreeExportRow> Rows);

public sealed class FamilyTreeExportPlanner
{
    private readonly FamilyTreeIndex _index;

    public FamilyTreeExportPlanner(FamilyTreeIndex index) =>
        _index = index ?? throw new ArgumentNullException(nameof(index));

    public FamilyTreeExportPlan Create(string probandId, string ancestorNumber, int ancestorDepth, int descendantDepth)
    {
        if (ancestorDepth is < 0 or > 5 || descendantDepth is < 0 or > 5 || ancestorDepth + descendantDepth > 5)
            throw new ArgumentOutOfRangeException(nameof(ancestorDepth), "Vælg højst seks generationer inklusive rodpersonen.");

        var root = _index.FindAncestor(probandId, ancestorNumber)
            ?? throw new ArgumentException("Anenummeret findes ikke i hovedtræet.", nameof(ancestorNumber));
        var path = new HashSet<string>(StringComparer.Ordinal) { root.Id };
        var ancestors = new List<Occurrence> { new(root, BigInteger.One, path) };
        var descendants = new List<Occurrence> { new(root, null, path, 0) };
        var descendantSlots = 1;
        var rows = new List<FamilyTreeExportRow>
        {
            new(0, "Rodperson", [new FamilyTreeExportCell(root, [BigInteger.One])])
        };

        for (var generation = 1; generation <= ancestorDepth; generation++)
        {
            var next = new List<Occurrence>();
            foreach (var entry in ancestors)
            {
                if (entry.Person is null) continue;
                var parents = _index.Parents(entry.Person.Id);
                if (!parents.HasCandidates) continue;
                AddAncestor(next, parents.Father, entry.Number!.Value * 2, entry.Path);
                AddAncestor(next, parents.Mother, entry.Number.Value * 2 + 1, entry.Path);
            }
            if (next.Count == 0) break;
            rows.Add(new FamilyTreeExportRow(-generation, AncestorLabel(generation), AncestorCells(next)));
            ancestors = next;
        }

        for (var generation = 1; generation <= descendantDepth; generation++)
        {
            var families = descendants.Select(entry => (Parent: entry,
                Children: entry.Person is null ? [] : _index.Children(entry.Person.Id)
                    .Where(child => !entry.Path.Contains(child.Person.Id)).ToArray())).ToArray();
            var columnsPerParent = families.Max(family => family.Children.Length);
            if (columnsPerParent == 0) break;
            if (descendantSlots > 1000 / columnsPerParent)
                throw new ArgumentException("Slægtstræet har for mange kolonnepositioner. Vælg færre generationer.");
            descendantSlots *= columnsPerParent;
            var next = new List<Occurrence>();
            foreach (var (entry, children) in families)
            {
                for (var index = 0; index < children.Length; index++)
                {
                    var child = children[index];
                    var childPath = new HashSet<string>(entry.Path, StringComparer.Ordinal) { child.Person.Id };
                    var firstPosition = entry.Position * columnsPerParent;
                    var offset = (columnsPerParent - children.Length) / 2;
                    next.Add(new Occurrence(child.Person, null, childPath, firstPosition + offset + index));
                }
            }
            var cells = next.Select(entry => new FamilyTreeExportCell(entry.Person, Array.Empty<BigInteger>())).ToArray();
            rows.Add(new FamilyTreeExportRow(generation, DescendantLabel(generation), cells)
            {
                GridSlots = descendantSlots,
                GridPositions = next.Select(entry => entry.Position).ToArray()
            });
            descendants = next;
        }

        return new FamilyTreeExportPlan(probandId, BigInteger.Parse(ancestorNumber, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            root, rows.OrderBy(row => row.Generation == 0 ? 0 : row.Generation > 0 ? row.Generation : descendantDepth - row.Generation).ToArray());
    }

    private static void AddAncestor(List<Occurrence> result, FamilyTreePerson? person, BigInteger number, HashSet<string> path)
    {
        if (person is null || path.Contains(person.Id))
        {
            result.Add(new Occurrence(null, number, path));
            return;
        }
        var nextPath = new HashSet<string>(path, StringComparer.Ordinal) { person.Id };
        result.Add(new Occurrence(person, number, nextPath));
    }

    private static IReadOnlyList<FamilyTreeExportCell> AncestorCells(List<Occurrence> occurrences)
    {
        return occurrences.OrderBy(entry => entry.Number)
            .Select(entry => new FamilyTreeExportCell(entry.Person, [entry.Number!.Value])).ToArray();
    }

    private static string AncestorLabel(int generation) => generation switch
    {
        1 => "Forældre", 2 => "Bedsteforældre", 3 => "Oldeforældre",
        4 => "Tipoldeforældre", 5 => "Tiptipoldeforældre",
        _ => $"Forfædre, {generation}. generation"
    };

    private static string DescendantLabel(int generation) => generation switch
    {
        1 => "Børn", 2 => "Børnebørn", 3 => "Oldebørn",
        _ => $"Efterkommere, {generation}. generation"
    };

    private sealed record Occurrence(FamilyTreePerson? Person, BigInteger? Number, HashSet<string> Path, int Position = 0);
}
