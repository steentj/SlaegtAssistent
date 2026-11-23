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
            ? "Ukendt"
            : string.Join(", ", AncestorNumbers.Select(value => value.ToString(CultureInfo.InvariantCulture)));
        var name = Person?.Name ?? "Ukendt";
        if (layout != FamilyTreeExportLayout.Detailed)
        {
            var birth = FamilyTreeIndex.FormatYear(Person?.BirthDate);
            var death = FamilyTreeIndex.FormatYear(Person?.DeathDate);
            return [$"Anenummer: {number}", name, $"{birth} – {death}"];
        }

        return [$"Anenummer: {number}", name,
            $"Født: {Person?.BirthDate ?? "Ukendt"}",
            $"Fødested: {Person?.BirthPlace ?? "Ukendt"}",
            $"Død: {Person?.DeathDate ?? "Ukendt"}",
            $"Dødssted: {Person?.DeathPlace ?? "Ukendt"}"];
    }
}

public sealed record FamilyTreeExportRow(
    int Generation, string Label, IReadOnlyList<FamilyTreeExportCell> Cells)
{
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
        var descendants = new List<Occurrence> { new(root, null, path) };
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

        var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), false);
        for (var generation = 1; generation <= descendantDepth; generation++)
        {
            var next = new List<Occurrence>();
            foreach (var entry in descendants)
            {
                if (entry.Person is null) continue;
                foreach (var child in _index.Children(entry.Person.Id))
                {
                    if (entry.Path.Contains(child.Person.Id)) continue;
                    var childPath = new HashSet<string>(entry.Path, StringComparer.Ordinal) { child.Person.Id };
                    next.Add(new Occurrence(child.Person, null, childPath));
                }
            }
            if (next.Count == 0) break;
            var cells = next.Select(entry => entry.Person!).DistinctBy(person => person.Id, StringComparer.Ordinal)
                .OrderBy(person => person.Name, comparer).ThenBy(person => person.Id, StringComparer.Ordinal)
                .Select(person => new FamilyTreeExportCell(person, Array.Empty<BigInteger>())).ToArray();
            rows.Add(new FamilyTreeExportRow(generation, DescendantLabel(generation), cells));
            descendants = next;
        }

        return new FamilyTreeExportPlan(probandId, BigInteger.Parse(ancestorNumber, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            root, rows.OrderBy(row => row.Generation).ToArray());
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
        var cells = new List<FamilyTreeExportCell>();
        foreach (var entry in occurrences.OrderBy(entry => entry.Number))
        {
            if (entry.Person is null)
            {
                cells.Add(new FamilyTreeExportCell(null, [entry.Number!.Value]));
                continue;
            }
            var existing = cells.FindIndex(cell => cell.Person?.Id == entry.Person.Id);
            if (existing < 0) cells.Add(new FamilyTreeExportCell(entry.Person, [entry.Number!.Value]));
            else cells[existing] = cells[existing] with { AncestorNumbers = cells[existing].AncestorNumbers.Append(entry.Number!.Value).ToArray() };
        }
        return cells;
    }

    private static string AncestorLabel(int generation) => generation switch
    {
        1 => "Forældre", 2 => "Bedsteforældre", 3 => "Oldeforældre",
        _ => $"Forfædre, {generation}. generation"
    };

    private static string DescendantLabel(int generation) => generation switch
    {
        1 => "Børn", 2 => "Børnebørn", 3 => "Oldebørn",
        _ => $"Efterkommere, {generation}. generation"
    };

    private sealed record Occurrence(FamilyTreePerson? Person, BigInteger? Number, HashSet<string> Path);
}
