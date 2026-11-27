using System.Text.Json;

namespace SlaegtsAssistent.Core.Biography;

public enum BiographyDifferenceKind
{
    Changed,
    Added,
    Removed,
}

[Flags]
public enum BiographyDifferenceCause
{
    None = 0,
    Gedcom = 1,
    Template = 2,
    BaselineMigration = 4,
}

public sealed record BiographyStructuredDifference(
    string Path,
    string Label,
    string? DocumentValue,
    string? ApprovedValue,
    string? ImportedValue,
    BiographyDifferenceKind Kind,
    BiographyDifferenceCause Causes);

public sealed class BiographyStructuredDifferenceService
{
    public IReadOnlyList<BiographyStructuredDifference> Compare(
        BiographyReconciliationState state,
        bool templateChanged = false,
        bool requiresMigration = false)
    {
        ArgumentNullException.ThrowIfNull(state);

        var result = new List<BiographyStructuredDifference>();
        if (state.Approved is { } approved)
        {
            AddPersonScalars(result, state, approved.Person, state.Imported.Person);
            AddSet(result, "relations.parents", "Forælder", approved.ParentRecordIds, state.Imported.ParentRecordIds);
            AddSet(result, "relations.children", "Barn", approved.ChildRecordIds, state.Imported.ChildRecordIds);
            AddPersonNotes(result, approved.Person, state.Imported.Person);
            AddKeyed(result, "person.childhoodFamilyNotes", "Familienotat",
                approved.Person.ChildhoodFamilyNotes ?? [], state.Imported.Person.ChildhoodFamilyNotes ?? [],
                family => family.RecordId);
            AddIdentifiedOrdered(result, "person.events", "Personhændelse", approved.Person.Events, state.Imported.Person.Events, item => item.Identity);
            AddIdentifiedOrdered(result, "person.census", "Folketælling", approved.Person.Census, state.Imported.Person.Census, item => item.Identity);
            AddKeyed(result, "person.sources", "Kilde", approved.Person.Sources, state.Imported.Person.Sources, SourceKey);
            AddIdentifiedOrdered(result, "person.media", "Medie", approved.Person.Media, state.Imported.Person.Media, MediaKey);
            AddKeyed(result, "families", "Familie", approved.Families, state.Imported.Families, family => family.RecordId);
            AddSubmitter(result, approved.Submitter, state.Imported.Submitter);
        }

        var migrationRequired = requiresMigration ||
                                state.Status is BiographyBaselineStatus.Missing or BiographyBaselineStatus.UnsupportedVersion;
        if (migrationRequired)
        {
            result.Add(new BiographyStructuredDifference(
                "migration.markers", "Dokumentmigrering", null, null,
                state.Status == BiographyBaselineStatus.UnsupportedVersion
                    ? "Ukendt baselineversion kræver manuel migrering"
                    : "Manglende markører eller baseline kræver manuel migrering",
                BiographyDifferenceKind.Added,
                BiographyDifferenceCause.BaselineMigration |
                (templateChanged ? BiographyDifferenceCause.Template : BiographyDifferenceCause.None)));
        }
        else if (templateChanged)
        {
            result.Add(new BiographyStructuredDifference(
                "template", "Skabelon", null, "Nuværende skabelon", "Ny skabelon",
                BiographyDifferenceKind.Changed, BiographyDifferenceCause.Template));
        }

        return result.OrderBy(item => item.Path, StringComparer.Ordinal).ToArray();
    }

    private static void AddPersonScalars(
        ICollection<BiographyStructuredDifference> result,
        BiographyReconciliationState state,
        CanonicalPersonData approved,
        CanonicalPersonData imported)
    {
        AddScalar(result, "person.fullName", "Navn", state.DocumentFacts.FullName, approved.FullName, imported.FullName);
        AddScalar(result, "person.sex", "Køn", state.DocumentFacts.Sex, approved.Sex, imported.Sex);
        AddScalar(result, "person.birthDate", "Fødselsdato", state.DocumentFacts.BirthDate, approved.BirthDate, imported.BirthDate);
        AddScalar(result, "person.birthPlace", "Fødested", state.DocumentFacts.BirthPlace, approved.BirthPlace, imported.BirthPlace);
        AddScalar(result, "person.deathDate", "Dødsdato", state.DocumentFacts.DeathDate, approved.DeathDate, imported.DeathDate);
        AddScalar(result, "person.deathPlace", "Dødssted", state.DocumentFacts.DeathPlace, approved.DeathPlace, imported.DeathPlace);
        AddScalar(result, "person.createdDate", "Oprettet dato", null, approved.CreatedDate, imported.CreatedDate);
    }

    private static void AddSubmitter(
        ICollection<BiographyStructuredDifference> result,
        CanonicalSubmitterData? approved,
        CanonicalSubmitterData? imported)
    {
        AddScalar(result, "submitter.recordId", "Afsender-id", null, approved?.RecordId, imported?.RecordId);
        AddScalar(result, "submitter.name", "Afsendernavn", null, approved?.Name, imported?.Name);
        AddScalar(result, "submitter.address", "Afsenderadresse", null, approved?.Address, imported?.Address);
        AddScalar(result, "submitter.phone", "Afsendertelefon", null, approved?.Phone, imported?.Phone);
        AddScalar(result, "submitter.email", "Afsender-e-mail", null, approved?.Email, imported?.Email);
        AddScalar(result, "submitter.website", "Afsenderwebsted", null, approved?.Website, imported?.Website);
        AddScalar(result, "submitter.language", "Afsendersprog", null, approved?.Language, imported?.Language);
    }

    private static void AddScalar(
        ICollection<BiographyStructuredDifference> result,
        string path,
        string label,
        string? documentValue,
        string? approvedValue,
        string? importedValue)
    {
        if (!string.Equals(approvedValue, importedValue, StringComparison.Ordinal))
        {
            result.Add(new BiographyStructuredDifference(
                path, label, documentValue, approvedValue, importedValue,
                Kind(approvedValue, importedValue), BiographyDifferenceCause.Gedcom));
        }
    }

    private static void AddSet(
        ICollection<BiographyStructuredDifference> result,
        string path,
        string label,
        IReadOnlyList<string> approved,
        IReadOnlyList<string> imported)
    {
        foreach (var value in approved.Concat(imported).Distinct(StringComparer.Ordinal))
        {
            var inApproved = approved.Contains(value, StringComparer.Ordinal);
            var inImported = imported.Contains(value, StringComparer.Ordinal);
            if (inApproved == inImported)
            {
                continue;
            }

            result.Add(new BiographyStructuredDifference(
                $"{path}[{value}]", label, null,
                inApproved ? value : null, inImported ? value : null,
                inImported ? BiographyDifferenceKind.Added : BiographyDifferenceKind.Removed,
                BiographyDifferenceCause.Gedcom));
        }
    }

    private static void AddPersonNotes(
        ICollection<BiographyStructuredDifference> result,
        CanonicalPersonData approved,
        CanonicalPersonData imported)
    {
        var oldNotes = NoteEntries(approved);
        var newNotes = NoteEntries(imported);
        for (var index = 0; index < Math.Max(oldNotes.Count, newNotes.Count); index++)
        {
            var oldNote = index < oldNotes.Count ? oldNotes[index] : null;
            var newNote = index < newNotes.Count ? newNotes[index] : null;
            if (string.Equals(oldNote?.Text, newNote?.Text, StringComparison.Ordinal)
                && string.Equals(Display(oldNote?.Details), Display(newNote?.Details), StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(new BiographyStructuredDifference(
                $"person.notes[{index}]", "Notat", null, DescribeNote(oldNote), DescribeNote(newNote),
                Kind(oldNote, newNote), BiographyDifferenceCause.Gedcom));
        }
    }

    internal static IReadOnlyList<NoteEntry> NoteEntries(CanonicalPersonData person) =>
        person.Notes.Select((text, index) => new NoteEntry(text,
            person.NoteDetails is { } details && index < details.Count ? details[index] : null)).ToArray();

    private static string? DescribeNote(NoteEntry? note) => note is null ? null
        : note.Details is null ? note.Text
        : string.Join("\n\n", note.Details.Parts.Select(part =>
            part.Text + (part.Timestamp is null ? "" : $" ({NoteTimestampFormatter.Format(part.Timestamp)})")));

    internal sealed record NoteEntry(string Text, CanonicalNoteDetails? Details);

    private static void AddIdentifiedOrdered<T>(
        ICollection<BiographyStructuredDifference> result,
        string path,
        string label,
        IReadOnlyList<T> approved,
        IReadOnlyList<T> imported,
        Func<T, string> identity)
        where T : class
    {
        foreach (var pair in PairOrderedItems(path, approved, imported, identity))
        {
            AddObject(result, pair.Path, label, pair.Approved, pair.Imported);
        }
    }

    internal static IEnumerable<(string Path, T? Approved, T? Imported)> PairOrderedItems<T>(
        string path,
        IReadOnlyList<T> approved,
        IReadOnlyList<T> imported,
        Func<T, string> identity)
        where T : class
    {
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < Math.Max(approved.Count, imported.Count); index++)
        {
            var oldValue = index < approved.Count ? approved[index] : null;
            var newValue = index < imported.Count ? imported[index] : null;
            var stableIdentity = identity(oldValue ?? newValue!);
            occurrences.TryGetValue(stableIdentity, out var count);
            var occurrence = count + 1;
            occurrences[stableIdentity] = occurrence;

            // Suffikset står uden for klammen, så det er adskilt fra identitetens eget suffiks.
            var fieldPath = $"{path}[{stableIdentity}]";
            yield return (occurrence == 1 ? fieldPath : $"{fieldPath}#{occurrence}", oldValue, newValue);
        }
    }

    private static void AddKeyed<T>(
        ICollection<BiographyStructuredDifference> result,
        string path,
        string label,
        IReadOnlyList<T> approved,
        IReadOnlyList<T> imported,
        Func<T, string> keySelector)
        where T : notnull
    {
        var oldItems = BuildKeyed(approved, keySelector);
        var newItems = BuildKeyed(imported, keySelector);
        foreach (var key in oldItems.Keys.Concat(newItems.Keys).Distinct(StringComparer.Ordinal))
        {
            oldItems.TryGetValue(key, out var oldValue);
            newItems.TryGetValue(key, out var newValue);
            AddObject(result, $"{path}[{key}]", label, oldValue, newValue);
        }
    }

    internal static IReadOnlyDictionary<string, T> BuildKeyed<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector)
        where T : notnull
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var baseKey = keySelector(item);
            occurrences.TryGetValue(baseKey, out var occurrence);
            occurrence++;
            occurrences[baseKey] = occurrence;
            result[occurrence == 1 ? baseKey : $"{baseKey}#{occurrence}"] = item;
        }

        return result;
    }

    private static void AddObject<T>(
        ICollection<BiographyStructuredDifference> result,
        string path,
        string label,
        T? approved,
        T? imported)
    {
        var approvedJson = Display(approved);
        var importedJson = Display(imported);
        if (string.Equals(approvedJson, importedJson, StringComparison.Ordinal))
        {
            return;
        }

        result.Add(new BiographyStructuredDifference(
            path, label, null, approvedJson, importedJson,
            Kind(approved, imported), BiographyDifferenceCause.Gedcom));
    }

    private static BiographyDifferenceKind Kind<T>(T? approved, T? imported)
    {
        if (approved is null)
        {
            return BiographyDifferenceKind.Added;
        }

        return imported is null ? BiographyDifferenceKind.Removed : BiographyDifferenceKind.Changed;
    }

    private static string? Display<T>(T? value) => value is null
        ? null
        : value is string text
            ? text
            : JsonSerializer.Serialize(value, value.GetType(), BiographyDocumentJsonContext.Default);

    internal static string SourceKey(CanonicalSourceData source) => source.RecordId ?? source.Identity;

    internal static string MediaKey(CanonicalMediaData media) => media.RecordId ?? media.File ?? media.Identity;
}

public sealed class BiographySnapshotDecisionService
{
    public CanonicalBiographySnapshot Apply(
        CanonicalBiographySnapshot approved,
        CanonicalBiographySnapshot imported,
        IReadOnlyDictionary<string, bool> useImportedByPath)
    {
        ArgumentNullException.ThrowIfNull(approved);
        ArgumentNullException.ThrowIfNull(imported);
        ArgumentNullException.ThrowIfNull(useImportedByPath);

        var selectedNotes = SelectOrdered("person.notes",
            BiographyStructuredDifferenceService.NoteEntries(approved.Person),
            BiographyStructuredDifferenceService.NoteEntries(imported.Person));
        var selectedPerson = approved.Person with
        {
            FullName = Pick("person.fullName", approved.Person.FullName, imported.Person.FullName),
            Sex = Pick("person.sex", approved.Person.Sex, imported.Person.Sex),
            BirthDate = Pick("person.birthDate", approved.Person.BirthDate, imported.Person.BirthDate),
            BirthPlace = Pick("person.birthPlace", approved.Person.BirthPlace, imported.Person.BirthPlace),
            DeathDate = Pick("person.deathDate", approved.Person.DeathDate, imported.Person.DeathDate),
            DeathPlace = Pick("person.deathPlace", approved.Person.DeathPlace, imported.Person.DeathPlace),
            CreatedDate = Pick("person.createdDate", approved.Person.CreatedDate, imported.Person.CreatedDate),
            Notes = selectedNotes.Select(note => note.Text).ToArray(),
            NoteDetails = selectedNotes.Any(note => note.Details is not null)
                ? selectedNotes.Select(note => note.Details).ToArray()
                : null,
            ChildhoodFamilyNotes = SelectChildhoodFamilyNotes(),
            Events = SelectIdentifiedOrdered("person.events", approved.Person.Events, imported.Person.Events, item => item.Identity),
            Census = SelectIdentifiedOrdered("person.census", approved.Person.Census, imported.Person.Census, item => item.Identity),
            Sources = SelectKeyed("person.sources", approved.Person.Sources, imported.Person.Sources, BiographyStructuredDifferenceService.SourceKey)
                .OrderBy(item => item.Identity, StringComparer.Ordinal)
                .ThenBy(item => item.RecordId, StringComparer.Ordinal)
                .ThenBy(item => item.Page, StringComparer.Ordinal)
                .ToArray(),
            Media = SelectIdentifiedOrdered("person.media", approved.Person.Media, imported.Person.Media, BiographyStructuredDifferenceService.MediaKey),
        };

        return approved with
        {
            Person = selectedPerson,
            ParentRecordIds = SelectSet("relations.parents", approved.ParentRecordIds, imported.ParentRecordIds),
            ChildRecordIds = SelectSet("relations.children", approved.ChildRecordIds, imported.ChildRecordIds),
            Families = SelectKeyed("families", approved.Families, imported.Families, family => family.RecordId),
            Submitter = SelectSubmitter(approved.Submitter, imported.Submitter),
        };

        T? Pick<T>(string path, T? oldValue, T? newValue) =>
            UseImported(path) ? newValue : oldValue;

        bool UseImported(string path) =>
            useImportedByPath.TryGetValue(path, out var selected) && selected;

        IReadOnlyList<CanonicalChildhoodFamilyNotes>? SelectChildhoodFamilyNotes()
        {
            var notes = SelectKeyed("person.childhoodFamilyNotes",
                approved.Person.ChildhoodFamilyNotes ?? [], imported.Person.ChildhoodFamilyNotes ?? [],
                family => family.RecordId);
            return notes.Count == 0 ? null : notes;
        }

        CanonicalSubmitterData? SelectSubmitter(CanonicalSubmitterData? oldValue, CanonicalSubmitterData? newValue)
        {
            if (oldValue is null || newValue is null)
            {
                return UseImported("submitter.recordId") || UseImported("submitter.name")
                    ? newValue
                    : oldValue;
            }

            return oldValue with
            {
                RecordId = Pick("submitter.recordId", oldValue.RecordId, newValue.RecordId),
                Name = Pick("submitter.name", oldValue.Name, newValue.Name),
                Address = Pick("submitter.address", oldValue.Address, newValue.Address),
                Phone = Pick("submitter.phone", oldValue.Phone, newValue.Phone),
                Email = Pick("submitter.email", oldValue.Email, newValue.Email),
                Website = Pick("submitter.website", oldValue.Website, newValue.Website),
                Language = Pick("submitter.language", oldValue.Language, newValue.Language),
            };
        }

        IReadOnlyList<string> SelectSet(string path, IReadOnlyList<string> oldItems, IReadOnlyList<string> newItems)
        {
            return oldItems.Concat(newItems)
                .Distinct(StringComparer.Ordinal)
                .Where(value => UseImported($"{path}[{value}]")
                    ? newItems.Contains(value, StringComparer.Ordinal)
                    : oldItems.Contains(value, StringComparer.Ordinal))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        IReadOnlyList<T> SelectOrdered<T>(string path, IReadOnlyList<T> oldItems, IReadOnlyList<T> newItems)
        {
            var result = new List<T>();
            for (var index = 0; index < Math.Max(oldItems.Count, newItems.Count); index++)
            {
                var useNew = UseImported($"{path}[{index}]");
                if (useNew && index < newItems.Count)
                {
                    result.Add(newItems[index]);
                }
                else if (!useNew && index < oldItems.Count)
                {
                    result.Add(oldItems[index]);
                }
            }

            return result;
        }

        IReadOnlyList<T> SelectIdentifiedOrdered<T>(
            string path,
            IReadOnlyList<T> oldItems,
            IReadOnlyList<T> newItems,
            Func<T, string> identity)
            where T : class
        {
            var result = new List<T>();
            foreach (var pair in BiographyStructuredDifferenceService.PairOrderedItems(path, oldItems, newItems, identity))
            {
                var selected = UseImported(pair.Path) ? pair.Imported : pair.Approved;
                if (selected is not null)
                {
                    result.Add(selected);
                }
            }

            return result;
        }

        IReadOnlyList<T> SelectKeyed<T>(
            string path,
            IReadOnlyList<T> oldItems,
            IReadOnlyList<T> newItems,
            Func<T, string> keySelector)
            where T : notnull
        {
            var oldByKey = BiographyStructuredDifferenceService.BuildKeyed(oldItems, keySelector);
            var newByKey = BiographyStructuredDifferenceService.BuildKeyed(newItems, keySelector);
            var result = new List<T>();
            foreach (var key in oldByKey.Keys.Concat(newByKey.Keys).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            {
                var source = UseImported($"{path}[{key}]") ? newByKey : oldByKey;
                if (source.TryGetValue(key, out var item))
                {
                    result.Add(item);
                }
            }

            return result;
        }
    }
}
