using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed record CanonicalBiographySnapshot(
    int Version,
    CanonicalPersonData Person,
    IReadOnlyList<string> ParentRecordIds,
    IReadOnlyList<string> ChildRecordIds,
    IReadOnlyList<CanonicalFamilyData> Families,
    CanonicalSubmitterData? Submitter)
{
    public const int CurrentVersion = 1;

    public string ToCanonicalJson()
    {
        return JsonSerializer.Serialize(this, CanonicalBiographyJsonContext.Default.CanonicalBiographySnapshot);
    }

    public string ComputeFingerprint()
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ToCanonicalJson())));
    }

    public static CanonicalBiographySnapshot Create(Person person, Submitter? submitter = null)
    {
        ArgumentNullException.ThrowIfNull(person);

        return new CanonicalBiographySnapshot(
            CurrentVersion,
            new CanonicalPersonData(
                Normalize(person.RecordId)!,
                Normalize(person.FullName),
                Normalize(person.Sex),
                Normalize(person.BirthDate),
                Normalize(person.BirthPlace),
                Normalize(person.DeathDate),
                Normalize(person.DeathPlace),
                person.Notes.Select(Normalize).Where(value => value is not null).Select(value => value!).ToArray(),
                CreateEvents(person.Events),
                CreateCensusList(person.Census),
                SortSources(person.Sources),
                CreateMediaList(person.Media)),
            SortRecordIds(person.Parents),
            SortRecordIds(person.Children),
            person.Families
                .OrderBy(family => family.RecordId, StringComparer.Ordinal)
                .DistinctBy(family => family.RecordId, StringComparer.Ordinal)
                .Select(CreateFamily)
                .ToArray(),
            submitter is null ? null : new CanonicalSubmitterData(
                Normalize(submitter.RecordId),
                Normalize(submitter.Name),
                Normalize(submitter.Address),
                Normalize(submitter.Phone),
                Normalize(submitter.Email),
                Normalize(submitter.Website),
                Normalize(submitter.Language)));
    }

    private static IReadOnlyList<string> SortRecordIds(IEnumerable<Person> people)
    {
        return people.Select(person => Normalize(person.RecordId)!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(recordId => recordId, StringComparer.Ordinal)
            .ToArray();
    }

    private static CanonicalFamilyData CreateFamily(Family family)
    {
        return new CanonicalFamilyData(
            Normalize(family.RecordId)!,
            Normalize(family.Husband?.RecordId),
            Normalize(family.Wife?.RecordId),
            SortRecordIds(family.Children),
            family.Notes.Select(Normalize).Where(value => value is not null).Select(value => value!).ToArray(),
            CreateEvents(family.Events),
            SortSources(family.Sources));
    }

    private static IReadOnlyList<CanonicalEventData> CreateEvents(IEnumerable<GedcomEvent> events)
    {
        var canonicalEvents = events.Select(item =>
        {
            var sourceData = SortSources(item.Sources);
            var values = new[]
            {
                item.Tag, item.Value, item.Date, item.Place, item.Type, item.Note,
                string.Join("|", sourceData.Select(source => source.Identity)),
            };
            return new CanonicalEventData(
                StableIdentity("event", values),
                Normalize(item.Tag)!,
                item.Category.ToString(),
                Normalize(item.Value),
                Normalize(item.Date),
                Normalize(item.Place),
                Normalize(item.Type),
                Normalize(item.Note),
                sourceData);
        });
        return EnsureUniqueIdentities(
            canonicalEvents,
            item => item.Identity,
            (item, identity) => item with { Identity = identity });
    }

    private static CanonicalCensusData CreateCensus(Census census)
    {
        var sources = SortSources(census.Sources);
        return new CanonicalCensusData(
            StableIdentity("census", [census.Date, census.Place, census.Note, string.Join("|", sources.Select(source => source.Identity))]),
            Normalize(census.Date),
            Normalize(census.Place),
            Normalize(census.Note),
            sources);
    }

    private static IReadOnlyList<CanonicalCensusData> CreateCensusList(IEnumerable<Census> census)
    {
        return EnsureUniqueIdentities(
            census.Select(CreateCensus),
            item => item.Identity,
            (item, identity) => item with { Identity = identity });
    }

    private static IReadOnlyList<CanonicalSourceData> SortSources(IEnumerable<Source> sources)
    {
        var sortedSources = sources.Select(CreateSource)
            .OrderBy(source => source.Identity, StringComparer.Ordinal)
            .ThenBy(source => source.RecordId, StringComparer.Ordinal)
            .ThenBy(source => source.Page, StringComparer.Ordinal);
        return EnsureUniqueIdentities(
            sortedSources,
            item => item.Identity,
            (item, identity) => item with { Identity = identity });
    }

    private static CanonicalSourceData CreateSource(Source source)
    {
        var values = new[]
        {
            source.RecordId, source.Title, source.Author, source.Publication, source.Text,
            source.Repository, source.Page, source.Data, source.Date, source.Note,
        };
        return new CanonicalSourceData(
            StableIdentity("source", values),
            Normalize(source.RecordId),
            Normalize(source.Title),
            Normalize(source.Author),
            Normalize(source.Publication),
            Normalize(source.Text),
            Normalize(source.Repository),
            Normalize(source.Page),
            Normalize(source.Data),
            Normalize(source.Date),
            Normalize(source.Note));
    }

    private static CanonicalMediaData CreateMedia(Media media)
    {
        return new CanonicalMediaData(
            StableIdentity("media", [media.RecordId, media.File, media.Form, media.Title, media.Type, media.Note]),
            Normalize(media.RecordId),
            Normalize(media.File),
            Normalize(media.Form),
            Normalize(media.Title),
            Normalize(media.Type),
            Normalize(media.Note));
    }

    private static IReadOnlyList<CanonicalMediaData> CreateMediaList(IEnumerable<Media> media)
    {
        return EnsureUniqueIdentities(
            media.Select(CreateMedia),
            item => item.Identity,
            (item, identity) => item with { Identity = identity });
    }

    private static string StableIdentity(string kind, IEnumerable<string?> values)
    {
        var canonical = kind + "\n" + string.Join("\n", values.Select(value => Encode(Normalize(value))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..24];
    }

    private static IReadOnlyList<T> EnsureUniqueIdentities<T>(
        IEnumerable<T> items,
        Func<T, string> getIdentity,
        Func<T, string, T> setIdentity)
    {
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<T>();
        foreach (var item in items)
        {
            var baseIdentity = getIdentity(item);
            occurrences.TryGetValue(baseIdentity, out var occurrence);
            occurrence++;
            occurrences[baseIdentity] = occurrence;
            result.Add(occurrence == 1
                ? item
                : setIdentity(item, $"{baseIdentity}#{occurrence}"));
        }

        return result;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Normalize(NormalizationForm.FormC);
    }

    private static string Encode(string? value) => value is null ? "<null>" : $"{value.Length}:{value}";
}

public sealed record CanonicalPersonData(
    string RecordId,
    string? FullName,
    string? Sex,
    string? BirthDate,
    string? BirthPlace,
    string? DeathDate,
    string? DeathPlace,
    IReadOnlyList<string> Notes,
    IReadOnlyList<CanonicalEventData> Events,
    IReadOnlyList<CanonicalCensusData> Census,
    IReadOnlyList<CanonicalSourceData> Sources,
    IReadOnlyList<CanonicalMediaData> Media);

public sealed record CanonicalFamilyData(
    string RecordId,
    string? HusbandRecordId,
    string? WifeRecordId,
    IReadOnlyList<string> ChildRecordIds,
    IReadOnlyList<string> Notes,
    IReadOnlyList<CanonicalEventData> Events,
    IReadOnlyList<CanonicalSourceData> Sources);

public sealed record CanonicalEventData(
    string Identity,
    string Tag,
    string Category,
    string? Value,
    string? Date,
    string? Place,
    string? Type,
    string? Note,
    IReadOnlyList<CanonicalSourceData> Sources);

public sealed record CanonicalCensusData(
    string Identity,
    string? Date,
    string? Place,
    string? Note,
    IReadOnlyList<CanonicalSourceData> Sources);

public sealed record CanonicalSourceData(
    string Identity,
    string? RecordId,
    string? Title,
    string? Author,
    string? Publication,
    string? Text,
    string? Repository,
    string? Page,
    string? Data,
    string? Date,
    string? Note);

public sealed record CanonicalMediaData(
    string Identity,
    string? RecordId,
    string? File,
    string? Form,
    string? Title,
    string? Type,
    string? Note);

public sealed record CanonicalSubmitterData(
    string? RecordId,
    string? Name,
    string? Address,
    string? Phone,
    string? Email,
    string? Website,
    string? Language);
