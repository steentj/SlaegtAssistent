using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.FamilyTrees;

public sealed record FamilyTreePerson(string Id, string Name, string BirthDate, string BirthPlace, string DeathDate, string DeathPlace);
public sealed record FamilyTreeChild(FamilyTreePerson Person, string? Relation);
public sealed record FamilyTreeParents(bool HasCandidates, FamilyTreePerson? Father, FamilyTreePerson? Mother, string? Explanation);

public sealed class FamilyTreeIndex
{
    private readonly Dictionary<string, FamilyTreePerson> _people;
    private readonly Dictionary<string, FamilyTreeParents> _parents = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<FamilyTreeChild>> _children = new(StringComparer.Ordinal);

    public FamilyTreeIndex(FamilyTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        _people = tree.People.ToDictionary(person => person.RecordId, person => new FamilyTreePerson(
            person.RecordId, FormatName(person), Value(person.BirthDate), Value(person.BirthPlace),
            Value(person.DeathDate), Value(person.DeathPlace)), StringComparer.Ordinal);
        var candidates = new Dictionary<string, List<Candidate>>(StringComparer.Ordinal);
        foreach (var family in tree.Families)
        {
            foreach (var child in family.Children)
            {
                var links = child.ChildhoodFamilyLinks.Where(link => link.FamilyId == family.RecordId).ToArray();
                var pedigree = links.Select(link => link.Pedigree?.Trim().ToLowerInvariant()).Where(value => !string.IsNullOrEmpty(value)).Distinct().ToArray();
                var roles = links.Select(link => link.AdoptedBy?.Trim().ToUpperInvariant()).Where(value => !string.IsNullOrEmpty(value)).Distinct().ToArray();
                var invalid = pedigree.Length > 1 || roles.Any(role => role is not ("HUSB" or "WIFE" or "BOTH"));
                var type = pedigree.Length == 1 ? pedigree[0] : null;
                var priority = invalid ? 0 : type switch { null => 1, "birth" => 2, "adopted" when roles.Length > 0 => 1, _ => 0 };
                var fatherAllowed = priority > 0 && !roles.Any(role => role is "HUSB" or "BOTH");
                var motherAllowed = priority > 0 && !roles.Any(role => role is "WIFE" or "BOTH");
                if (!candidates.TryGetValue(child.RecordId, out var list)) candidates[child.RecordId] = list = [];
                list.Add(new Candidate(family.Husband?.RecordId, family.Wife?.RecordId, priority, fatherAllowed, motherAllowed));
                AddChild(family.Husband, child, Relation(type, roles, "HUSB", invalid));
                AddChild(family.Wife, child, Relation(type, roles, "WIFE", invalid));
            }
        }
        foreach (var (id, list) in candidates)
        {
            var father = Resolve(list, true);
            var mother = Resolve(list, false);
            if (father is not null && mother is not null && !list.Any(item => item.Father == father && item.Mother == mother && item.FatherAllowed && item.MotherAllowed))
            {
                father = null;
                mother = null;
            }
            var hasCandidates = list.Any(item => item.Father is not null || item.Mother is not null);
            var explanation = hasCandidates && (father is null || mother is null) ? "En forældreplads er ukendt eller uafklaret." : null;
            _parents[id] = new FamilyTreeParents(hasCandidates, Find(father), Find(mother), explanation);
        }
        var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), false);
        foreach (var list in _children.Values)
            list.Sort((a, b) => { var result = comparer.Compare(a.Person.Name, b.Person.Name); return result != 0 ? result : StringComparer.Ordinal.Compare(a.Person.Id, b.Person.Id); });
    }

    public IReadOnlyCollection<FamilyTreePerson> People => _people.Values;
    public FamilyTreePerson? Find(string? id) => id is not null && _people.TryGetValue(id, out var person) ? person : null;
    public FamilyTreeParents Parents(string id) => _parents.GetValueOrDefault(id) ?? new(false, null, null, null);
    public IReadOnlyList<FamilyTreeChild> Children(string id) => _children.TryGetValue(id, out var children) ? children : [];

    public FamilyTreePerson? FindAncestor(string rootId, string input)
    {
        input = input.Trim();
        if (input.Length == 0 || input.Any(character => character is < '0' or > '9')) return null;
        input = input.TrimStart('0');
        if (input.Length == 0 || input.Length > _people.Count + 1 || !BigInteger.TryParse(input, out var number) || number < 1) return null;
        var steps = new Stack<bool>();
        while (number > 1) { steps.Push(!number.IsEven); number /= 2; }
        var person = Find(rootId);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (person is not null && steps.Count > 0)
        {
            if (!visited.Add(person.Id)) return null;
            var parents = Parents(person.Id);
            person = steps.Pop() ? parents.Mother : parents.Father;
        }
        return person is not null && visited.Add(person.Id) ? person : null;
    }

    public static string FormatName(Person person)
    {
        if (person.Names.Count == 0) return Value(person.FullName);
        var birth = person.Names.FirstOrDefault(name => string.Equals(name.Type, "birth", StringComparison.OrdinalIgnoreCase))
            ?? person.Names.FirstOrDefault(name => string.Equals(name.Type, "maiden", StringComparison.OrdinalIgnoreCase))
            ?? person.Names.FirstOrDefault(name => string.IsNullOrWhiteSpace(name.Type));
        var baseName = Value(birth?.FullName);
        var later = person.Names.Where(name => IsLaterName(name.Type)).Select(name => name.FullName.Trim())
            .Where(name => name.Length > 0 && !string.Equals(name, baseName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return later.Length == 0 ? baseName : $"{baseName} ({string.Join(", ", later)})";
    }

    public static string FormatYear(string? date)
    {
        if (string.IsNullOrWhiteSpace(date)) return "Ukendt";
        var match = Regex.Match(date.Trim(), @"^(?:(ABT|CAL|EST)\s+)?(?:(?:[0-9]{1,2}\s+)?(?:JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)\s+)?([0-9]{1,4})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? (match.Groups[1].Success ? "ca. " : "") + match.Groups[2].Value : "Ukendt";
    }

    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "Ukendt" : value.Trim();
    private static bool IsLaterName(string? type) => type is not null && new[] { "married", "immigrant", "name change", "navneændring", "gift" }.Contains(type.Trim(), StringComparer.OrdinalIgnoreCase);
    private static string? Resolve(List<Candidate> list, bool father)
    {
        var candidates = list.Where(item => father ? item.FatherAllowed && item.Father is not null : item.MotherAllowed && item.Mother is not null).ToArray();
        if (candidates.Length == 0) return null;
        var priority = candidates.Max(item => item.Priority);
        var ids = candidates.Where(item => item.Priority == priority).Select(item => father ? item.Father : item.Mother).Distinct(StringComparer.Ordinal).ToArray();
        return ids.Length == 1 ? ids[0] : null;
    }
    private void AddChild(Person? parent, Person child, string? relation)
    {
        if (parent is null || !_people.TryGetValue(child.RecordId, out var item)) return;
        if (!_children.TryGetValue(parent.RecordId, out var list)) _children[parent.RecordId] = list = [];
        var existing = list.FindIndex(entry => entry.Person.Id == child.RecordId);
        if (existing < 0) list.Add(new FamilyTreeChild(item, relation));
        else if (list[existing].Relation != relation) list[existing] = new FamilyTreeChild(item, "Flere relationstyper");
    }
    private static string? Relation(string? pedigree, string?[] roles, string role, bool invalid)
    {
        if (invalid) return "Uafklaret relation";
        if (roles.Any(value => value == role || value == "BOTH")) return "Adoptivbarn";
        if (pedigree == "adopted" && roles.Length > 0) return null;
        return pedigree switch { null or "birth" => null, "adopted" => "Adoptivbarn", "foster" => "Plejeforhold", "sealing" => "Forsegling", _ => "Uafklaret relation" };
    }
    private sealed record Candidate(string? Father, string? Mother, int Priority, bool FatherAllowed, bool MotherAllowed);
}
