namespace SlaegtsAssistent.Core.Domain;

public sealed class Person
{
    private readonly GedcomNoteCollection _notes = new();

    public Person(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            throw new ArgumentException("Record id is required.", nameof(recordId));
        }

        RecordId = recordId;
    }

    public string RecordId { get; }

    public string RawGedcom { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? Sex { get; set; }

    public string? BirthDate { get; set; }

    public string? BirthPlace { get; set; }

    public string? DeathDate { get; set; }

    public string? DeathPlace { get; set; }

    public string? CreatedDate { get; set; }

    public IList<Person> Parents { get; } = new List<Person>();

    public IList<Person> Children { get; } = new List<Person>();

    public IList<Family> Families { get; } = new List<Family>();

    internal IList<Family> ChildhoodFamilies { get; } = new List<Family>();

    public IEnumerable<GedcomEvent> FamilyEvents => Families.SelectMany(family => family.Events);

    public IList<Source> Sources { get; } = new List<Source>();

    public IList<string> Notes => _notes;

    internal IReadOnlyList<GedcomNoteValue> NoteValues => _notes.Values;

    internal void AddNote(GedcomNoteValue note) => _notes.Add(note);

    public IList<Media> Media { get; } = new List<Media>();

    public IList<GedcomEvent> Events { get; } = new List<GedcomEvent>();

    public IList<Census> Census { get; } = new List<Census>();
}
