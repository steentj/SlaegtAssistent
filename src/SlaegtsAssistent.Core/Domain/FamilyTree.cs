namespace SlaegtsAssistent.Core.Domain;

public sealed class FamilyTree
{
    private readonly Dictionary<string, Person> _peopleByRecordId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Source> _sourcesByRecordId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Media> _mediaByRecordId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Family> _familiesByRecordId = new(StringComparer.Ordinal);

    public IReadOnlyCollection<Person> People => _peopleByRecordId.Values;

    public IReadOnlyCollection<Family> Families => _familiesByRecordId.Values;

    public IReadOnlyCollection<Source> Sources => _sourcesByRecordId.Values;

    public IReadOnlyCollection<Media> Media => _mediaByRecordId.Values;

    public Submitter? Submitter { get; internal set; }

    public string? SubmitterRecordId { get; internal set; }

    public IList<GedcomDiagnostic> Diagnostics { get; } = new List<GedcomDiagnostic>();

    public GedcomImportReport ImportReport { get; internal set; } = GedcomImportReport.Empty;

    public string? SourceFilePath { get; internal set; }

    public Person? FindPerson(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return null;
        }

        return _peopleByRecordId.GetValueOrDefault(recordId);
    }

    public Source? FindSource(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return null;
        }

        return _sourcesByRecordId.GetValueOrDefault(recordId);
    }

    public Media? FindMedia(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return null;
        }

        return _mediaByRecordId.GetValueOrDefault(recordId);
    }

    public Family? FindFamily(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return null;
        }

        return _familiesByRecordId.GetValueOrDefault(recordId);
    }

    internal Person GetOrAddPerson(string recordId)
    {
        if (!_peopleByRecordId.TryGetValue(recordId, out var person))
        {
            person = new Person(recordId);
            _peopleByRecordId.Add(recordId, person);
        }

        return person;
    }

    internal bool TryGetPerson(string recordId, out Person person)
    {
        return _peopleByRecordId.TryGetValue(recordId, out person!);
    }

    internal Source GetOrAddSource(string recordId)
    {
        if (!_sourcesByRecordId.TryGetValue(recordId, out var source))
        {
            source = new Source(recordId);
            _sourcesByRecordId.Add(recordId, source);
        }

        return source;
    }

    internal Media GetOrAddMedia(string recordId)
    {
        if (!_mediaByRecordId.TryGetValue(recordId, out var media))
        {
            media = new Media(recordId);
            _mediaByRecordId.Add(recordId, media);
        }

        return media;
    }

    internal Family GetOrAddFamily(string recordId)
    {
        if (!_familiesByRecordId.TryGetValue(recordId, out var family))
        {
            family = new Family(recordId);
            _familiesByRecordId.Add(recordId, family);
        }

        return family;
    }
}
