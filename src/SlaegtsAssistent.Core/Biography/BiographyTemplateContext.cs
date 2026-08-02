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
        string? mediaBaseDirectory = null)
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
            .Concat(person.FamilyEvents.SelectMany(@event => @event.Sources))
            .Concat(person.Census.SelectMany(item => item.Sources))
            .Select(SourceTemplateContext.FromSource)
            .DistinctBy(source => source.Key, StringComparer.Ordinal)
            .ToArray();
        var media = person.Media
            .Select(item => MediaTemplateContext.FromMedia(item, mediaBaseDirectory))
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
    string? Date)
{
    public static SourceTemplateContext FromSource(Source source)
    {
        var key = source.RecordId
            ?? string.Join("|", source.Title, source.Author, source.Page, source.Date);
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
            source.Date);
    }
}

public sealed record MediaTemplateContext(
    string? RecordId,
    string? File,
    string? RelativeFile,
    string? Form,
    string? Title,
    string? Type,
    string? Note)
{
    public static MediaTemplateContext FromMedia(Media media, string? mediaBaseDirectory)
    {
        var relativeFile = media.File;
        if (!string.IsNullOrWhiteSpace(relativeFile) && Path.IsPathRooted(relativeFile))
        {
            relativeFile = string.IsNullOrWhiteSpace(mediaBaseDirectory)
                ? Path.GetFileName(relativeFile)
                : Path.GetRelativePath(mediaBaseDirectory, relativeFile);
        }

        return new MediaTemplateContext(
            media.RecordId,
            media.File,
            relativeFile?.Replace('\\', '/'),
            media.Form,
            media.Title,
            media.Type,
            media.Note);
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
