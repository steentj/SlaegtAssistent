using System.Text;
using Patagames.GedcomNetSdk;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Gedcom;

public sealed class GedcomLoader : IGedcomLoader
{
    public FamilyTree Load(string filePath, FamilyTree? existingTree = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new GedcomLoadException("A GEDCOM file path is required.");
        }

        if (!File.Exists(filePath))
        {
            throw new GedcomLoadException($"GEDCOM file was not found: '{filePath}'.");
        }

        try
        {
            var parsedGedcom = ParseGedcom(filePath);
            var tree = existingTree ?? new FamilyTree();

            MergeIntoTree(tree, parsedGedcom);

            return tree;
        }
        catch (GedcomLoadException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GedcomLoadException(
                $"Failed to load GEDCOM file '{filePath}'. {exception.Message}",
                exception);
        }
    }

    private static ParsedGedcom ParseGedcom(string filePath)
    {
        using var stream = CreateNormalizedLineEndingStream(filePath);
        using var parser = new Parser(stream);

        var people = new Dictionary<string, ParsedPerson>(StringComparer.Ordinal);
        var families = new List<ParsedFamily>();

        ParsedPerson? currentPerson = null;
        ParsedFamily? currentFamily = null;
        string? currentEventTag = null;

        while (parser.ReadLevel())
        {
            if (!parser.ReadTag())
            {
                break;
            }

            var level = parser.Level;
            var tag = parser.Tag;
            var value = parser.Value?.Trim();

            if (level == 0)
            {
                currentPerson = null;
                currentFamily = null;
                currentEventTag = null;

                switch (tag)
                {
                    case "INDI":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Malformed GEDCOM: INDI record without an id at line {parser.No}.");
                        }

                        if (!people.TryGetValue(value, out currentPerson))
                        {
                            currentPerson = new ParsedPerson(value);
                            people.Add(value, currentPerson);
                        }
                        break;

                    case "FAM":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Malformed GEDCOM: FAM record without an id at line {parser.No}.");
                        }

                        currentFamily = new ParsedFamily(value);
                        families.Add(currentFamily);
                        break;
                }

                continue;
            }

            if (currentPerson is not null)
            {
                ParsePersonLine(currentPerson, level, tag, value, ref currentEventTag);
                continue;
            }

            if (currentFamily is not null)
            {
                ParseFamilyLine(currentFamily, level, tag, value);
            }
        }

        return new ParsedGedcom(people.Values.ToList(), families);
    }

    private static void ParsePersonLine(
        ParsedPerson person,
        int level,
        string tag,
        string? value,
        ref string? currentEventTag)
    {
        if (level == 1)
        {
            currentEventTag = null;

            switch (tag)
            {
                case "NAME":
                    person.FullName = NormalizeName(value);
                    break;

                case "SEX":
                    person.Sex = NormalizeToken(value);
                    break;

                case "BIRT":
                case "DEAT":
                    currentEventTag = tag;
                    break;
            }

            return;
        }

        if (level != 2 || currentEventTag is null)
        {
            return;
        }

        if (tag == "DATE")
        {
            if (currentEventTag == "BIRT")
            {
                person.BirthDate = NormalizeToken(value);
            }
            else
            {
                person.DeathDate = NormalizeToken(value);
            }
        }
        else if (tag == "PLAC")
        {
            if (currentEventTag == "BIRT")
            {
                person.BirthPlace = NormalizeToken(value);
            }
            else
            {
                person.DeathPlace = NormalizeToken(value);
            }
        }
    }

    private static void ParseFamilyLine(ParsedFamily family, int level, string tag, string? value)
    {
        if (level != 1 || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        switch (tag)
        {
            case "HUSB":
                family.HusbandId = value;
                break;

            case "WIFE":
                family.WifeId = value;
                break;

            case "CHIL":
                family.ChildrenIds.Add(value);
                break;
        }
    }

    private static void MergeIntoTree(FamilyTree tree, ParsedGedcom parsedGedcom)
    {
        var importedRecordIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parsedPerson in parsedGedcom.People)
        {
            importedRecordIds.Add(parsedPerson.RecordId);

            var person = tree.GetOrAddPerson(parsedPerson.RecordId);
            person.FullName = parsedPerson.FullName;
            person.Sex = parsedPerson.Sex;
            person.BirthDate = parsedPerson.BirthDate;
            person.BirthPlace = parsedPerson.BirthPlace;
            person.DeathDate = parsedPerson.DeathDate;
            person.DeathPlace = parsedPerson.DeathPlace;
        }

        foreach (var recordId in importedRecordIds)
        {
            if (!tree.TryGetPerson(recordId, out var person))
            {
                continue;
            }

            UnlinkPerson(person);
        }

        foreach (var family in parsedGedcom.Families)
        {
            var parents = new List<Person>(2);

            if (!string.IsNullOrWhiteSpace(family.HusbandId)
                && tree.TryGetPerson(family.HusbandId, out var husband))
            {
                parents.Add(husband);
            }

            if (!string.IsNullOrWhiteSpace(family.WifeId)
                && tree.TryGetPerson(family.WifeId, out var wife)
                && parents.All(parent => parent.RecordId != wife.RecordId))
            {
                parents.Add(wife);
            }

            foreach (var childId in family.ChildrenIds)
            {
                if (!tree.TryGetPerson(childId, out var child))
                {
                    continue;
                }

                foreach (var parent in parents)
                {
                    AddIfMissing(parent.Children, child);
                    AddIfMissing(child.Parents, parent);
                }
            }
        }
    }

    private static void UnlinkPerson(Person person)
    {
        foreach (var parent in person.Parents.ToList())
        {
            parent.Children.Remove(person);
        }

        foreach (var child in person.Children.ToList())
        {
            child.Parents.Remove(person);
        }

        person.Parents.Clear();
        person.Children.Clear();
    }

    private static void AddIfMissing(IList<Person> people, Person personToAdd)
    {
        if (people.Any(existing => existing.RecordId == personToAdd.RecordId))
        {
            return;
        }

        people.Add(personToAdd);
    }

    private static MemoryStream CreateNormalizedLineEndingStream(string filePath)
    {
        var fileContent = File.ReadAllText(filePath);
        var normalizedLineEndings = NormalizeLineEndings(fileContent);
        return new MemoryStream(Encoding.UTF8.GetBytes(normalizedLineEndings));
    }

    private static string NormalizeLineEndings(string text)
    {
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", "\r\n", StringComparison.Ordinal);

        return normalized.EndsWith("\r\n", StringComparison.Ordinal)
            ? normalized
            : normalized + "\r\n";
    }

    private static string? NormalizeName(string? value)
    {
        var normalized = NormalizeToken(value);
        if (normalized is null)
        {
            return null;
        }

        var noSlash = normalized.Replace("/", string.Empty, StringComparison.Ordinal);
        var compact = string.Join(' ', noSlash.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return compact.Length == 0 ? null : compact;
    }

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private sealed record ParsedGedcom(IReadOnlyCollection<ParsedPerson> People, IReadOnlyCollection<ParsedFamily> Families);

    private sealed class ParsedPerson
    {
        public ParsedPerson(string recordId)
        {
            RecordId = recordId;
        }

        public string RecordId { get; }

        public string? FullName { get; set; }

        public string? Sex { get; set; }

        public string? BirthDate { get; set; }

        public string? BirthPlace { get; set; }

        public string? DeathDate { get; set; }

        public string? DeathPlace { get; set; }
    }

    private sealed class ParsedFamily
    {
        public ParsedFamily(string recordId)
        {
            RecordId = recordId;
        }

        public string RecordId { get; }

        public string? HusbandId { get; set; }

        public string? WifeId { get; set; }

        public IList<string> ChildrenIds { get; } = new List<string>();
    }
}
