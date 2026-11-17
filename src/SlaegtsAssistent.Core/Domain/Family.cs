namespace SlaegtsAssistent.Core.Domain;

public sealed class Family
{
    private readonly GedcomNoteCollection _notes = new();

    public Family(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            throw new ArgumentException("Familie-id er påkrævet.", nameof(recordId));
        }

        RecordId = recordId;
    }

    public string RecordId { get; }

    public Person? Husband { get; internal set; }

    public Person? Wife { get; internal set; }

    public IList<Person> Children { get; } = new List<Person>();

    public IList<Source> Sources { get; } = new List<Source>();

    public IList<string> Notes => _notes;

    internal IReadOnlyList<GedcomNoteValue> NoteValues => _notes.Values;

    internal void AddNote(GedcomNoteValue note) => _notes.Add(note);

    public IList<GedcomEvent> Events { get; } = new List<GedcomEvent>();
}
