using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateContext
{
    public BiographyTemplateContext(
        PersonTemplateContext person,
        SubmitterTemplateContext? submitter,
        IReadOnlyList<EventTemplateContext> events,
        IReadOnlyList<EventTemplateContext> familyEvents,
        IReadOnlyList<CensusTemplateContext> census,
        IReadOnlyList<SourceTemplateContext> sources,
        IReadOnlyList<MediaTemplateContext> media)
    {
        Person = person;
        Submitter = submitter;
        Events = events;
        FamilyEvents = familyEvents;
        Census = census;
        Sources = sources;
        Media = media;
        AllEvents = events.Concat(familyEvents).ToArray();
    }

    public PersonTemplateContext Person { get; }

    public SubmitterTemplateContext? Submitter { get; }

    public IReadOnlyList<EventTemplateContext> Events { get; }

    public IReadOnlyList<EventTemplateContext> FamilyEvents { get; }

    public IReadOnlyList<EventTemplateContext> AllEvents { get; }

    public IReadOnlyList<CensusTemplateContext> Census { get; }

    public IReadOnlyList<SourceTemplateContext> Sources { get; }

    public IReadOnlyList<MediaTemplateContext> Media { get; }

    public static BiographyTemplateContext FromPerson(
        Person person,
        Submitter? submitter = null,
        string? mediaBaseDirectory = null,
        string? gedcomSourceDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(person);

        var events = person.Events
            .Select(EventTemplateContext.FromEvent)
            .ToArray();
        var familyEvents = person.FamilyEvents
            .Select(EventTemplateContext.FromEvent)
            .ToArray();
        var census = person.Census
            .Select(CensusTemplateContext.FromCensus)
            .ToArray();
        var sources = person.Sources
            .Concat(person.Events.SelectMany(@event => @event.Sources))
            .Concat(person.Families.SelectMany(family => family.Sources))
            .Concat(person.FamilyEvents.SelectMany(@event => @event.Sources))
            .Concat(person.Census.SelectMany(item => item.Sources))
            .Select(SourceTemplateContext.FromSource)
            .DistinctBy(source => source.Key, StringComparer.Ordinal)
            .ToArray();
        var media = person.Media
            .Select(item => MediaTemplateContext.FromMedia(
                item,
                mediaBaseDirectory,
                gedcomSourceDirectory))
            .ToArray();

        return new BiographyTemplateContext(
            PersonTemplateContext.FromPerson(person),
            submitter is null ? null : SubmitterTemplateContext.FromSubmitter(submitter),
            events,
            familyEvents,
            census,
            sources,
            media);
    }

    public static BiographyTemplateContext FromSnapshot(
        CanonicalBiographySnapshot snapshot,
        string? mediaBaseDirectory = null,
        IReadOnlyDictionary<string, string?>? personNames = null,
        string? gedcomSourceDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var person = snapshot.Person;
        var events = person.Events.Select(ToEvent).ToArray();
        var familyEvents = snapshot.Families.SelectMany(family => family.Events).Select(ToEvent).ToArray();
        var census = person.Census.Select(item => new CensusTemplateContext(
            item.Date,
            item.Place,
            item.Note,
            item.Sources.Select(ToSource).ToArray())).ToArray();
        var sources = person.Sources
            .Concat(person.Events.SelectMany(item => item.Sources))
            .Concat(snapshot.Families.SelectMany(family => family.Sources))
            .Concat(snapshot.Families.SelectMany(family => family.Events).SelectMany(item => item.Sources))
            .Concat(person.Census.SelectMany(item => item.Sources))
            .Select(ToSource)
            .DistinctBy(source => source.Key, StringComparer.Ordinal)
            .ToArray();
        var media = person.Media.Select(item =>
        {
            var resolution = string.IsNullOrWhiteSpace(mediaBaseDirectory)
                ? null
                : new BiographyMediaResolver().Resolve(
                    item.File,
                    gedcomSourceDirectory,
                    mediaBaseDirectory);
            var relativeFile = string.IsNullOrWhiteSpace(mediaBaseDirectory)
                ? item.File?.Replace('\\', '/')
                : resolution?.RelativePath;

            return new MediaTemplateContext(
                item.RecordId,
                item.File,
                relativeFile,
                item.Form,
                item.Title,
                item.Type,
                item.Note,
                resolution?.Diagnostic,
                resolution?.RequiresApproval ?? false);
        }).ToArray();
        var submitter = snapshot.Submitter is null
            ? null
            : new SubmitterTemplateContext(
                snapshot.Submitter.RecordId,
                snapshot.Submitter.Name,
                snapshot.Submitter.Address,
                snapshot.Submitter.Phone,
                snapshot.Submitter.Email,
                snapshot.Submitter.Website,
                snapshot.Submitter.Language);

        return new BiographyTemplateContext(
            new PersonTemplateContext(
                person.RecordId,
                person.FullName,
                person.Sex,
                person.BirthDate,
                person.BirthPlace,
                person.DeathDate,
                person.DeathPlace,
                snapshot.ParentRecordIds
                    .Select(recordId => new PersonReferenceTemplateContext(
                        recordId,
                        personNames is not null && personNames.TryGetValue(recordId, out var name)
                            ? name
                            : recordId))
                    .ToArray()),
            submitter,
            events,
            familyEvents,
            census,
            sources,
            media);

        static EventTemplateContext ToEvent(CanonicalEventData item)
        {
            var category = Enum.TryParse<GedcomEventCategory>(item.Category, out var parsed)
                ? parsed
                : GedcomEventCategory.Other;
            return new EventTemplateContext(
                item.Tag,
                category,
                item.Value,
                item.Date,
                item.Place,
                item.Type,
                item.Note,
                item.Sources.Select(ToSource).ToArray());
        }

        static SourceTemplateContext ToSource(CanonicalSourceData item) => new(
            item.Identity,
            item.RecordId,
            item.Title,
            item.Author,
            item.Publication,
            item.Text,
            item.Repository,
            item.Page,
            item.Data,
            item.Date,
            item.Note);
    }
}

public sealed record PersonTemplateContext(
    string RecordId,
    string? FullName,
    string? Sex,
    string? BirthDate,
    string? BirthPlace,
    string? DeathDate,
    string? DeathPlace,
    IReadOnlyList<PersonReferenceTemplateContext> Parents)
{
    public string ParentNames => string.Join(", ", Parents
        .Select(parent => parent.FullName)
        .Where(name => !string.IsNullOrWhiteSpace(name)));

    public static PersonTemplateContext FromPerson(Person person)
    {
        return new PersonTemplateContext(
            person.RecordId,
            person.FullName,
            person.Sex,
            person.BirthDate,
            person.BirthPlace,
            person.DeathDate,
            person.DeathPlace,
            person.Parents
                .Select(parent => new PersonReferenceTemplateContext(parent.RecordId, parent.FullName))
                .ToArray());
    }
}

public sealed record PersonReferenceTemplateContext(string RecordId, string? FullName);

public sealed record EventTemplateContext(
    string Tag,
    GedcomEventCategory Category,
    string? Value,
    string? Date,
    string? Place,
    string? Type,
    string? Note,
    IReadOnlyList<SourceTemplateContext> Sources)
{
    public static EventTemplateContext FromEvent(GedcomEvent @event)
    {
        return new EventTemplateContext(
            @event.Tag,
            @event.Category,
            @event.Value,
            @event.Date,
            @event.Place,
            @event.Type,
            @event.Note,
            @event.Sources.Select(SourceTemplateContext.FromSource).ToArray());
    }
}

public sealed record CensusTemplateContext(
    string? Date,
    string? Place,
    string? Note,
    IReadOnlyList<SourceTemplateContext> Sources)
{
    public static CensusTemplateContext FromCensus(Census census)
    {
        return new CensusTemplateContext(
            census.Date,
            census.Place,
            census.Note,
            census.Sources.Select(SourceTemplateContext.FromSource).ToArray());
    }
}

public sealed record SourceTemplateContext(
    string Key,
    string? RecordId,
    string? Title,
    string? Author,
    string? Publication,
    string? Text,
    string? Repository,
    string? Page,
    string? Data,
    string? Date,
    string? Note)
{
    public static SourceTemplateContext FromSource(Source source)
    {
        var key = string.Join(
            "|",
            source.RecordId,
            source.Title,
            source.Author,
            source.Publication,
            source.Text,
            source.Repository,
            source.Page,
            source.Data,
            source.Date,
            source.Note);
        return new SourceTemplateContext(
            key,
            source.RecordId,
            source.Title,
            source.Author,
            source.Publication,
            source.Text,
            source.Repository,
            source.Page,
            source.Data,
            source.Date,
            source.Note);
    }
}

public sealed record MediaTemplateContext(
    string? RecordId,
    string? File,
    string? RelativeFile,
    string? Form,
    string? Title,
    string? Type,
    string? Note,
    string? Diagnostic = null,
    bool RequiresApproval = false)
{
    public static MediaTemplateContext FromMedia(
        Media media,
        string? mediaBaseDirectory,
        string? gedcomSourceDirectory = null)
    {
        BiographyMediaResolution? resolution = null;
        if (!string.IsNullOrWhiteSpace(mediaBaseDirectory))
        {
            resolution = new BiographyMediaResolver().Resolve(
                media.File,
                gedcomSourceDirectory,
                mediaBaseDirectory);
        }

        var relativeFile = string.IsNullOrWhiteSpace(mediaBaseDirectory)
            ? media.File?.Replace('\\', '/')
            : resolution?.RelativePath;

        return new MediaTemplateContext(
            media.RecordId,
            media.File,
            relativeFile?.Replace('\\', '/'),
            media.Form,
            media.Title,
            media.Type,
            media.Note,
            resolution?.Diagnostic,
            resolution?.RequiresApproval ?? false);
    }
}

public sealed record SubmitterTemplateContext(
    string? RecordId,
    string? Name,
    string? Address,
    string? Phone,
    string? Email,
    string? Website,
    string? Language)
{
    public static SubmitterTemplateContext FromSubmitter(Submitter submitter)
    {
        return new SubmitterTemplateContext(
            submitter.RecordId,
            submitter.Name,
            submitter.Address,
            submitter.Phone,
            submitter.Email,
            submitter.Website,
            submitter.Language);
    }
}
