namespace SlaegtsAssistent.Core.Domain;

public sealed class GedcomEvent
{
    public GedcomEvent(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Hændelsestag er påkrævet.", nameof(tag));
        }

        Tag = tag;
    }

    public string Tag { get; }

    public GedcomEventCategory Category { get; set; }

    public string? Value { get; set; }

    public string? Date { get; set; }

    public string? Place { get; set; }

    public string? Type { get; set; }

    internal GedcomNoteValue? NoteValue { get; set; }

    public string? Note
    {
        get => NoteValue?.Text;
        set => NoteValue = value is null ? null : new GedcomNoteValue(value);
    }

    public IList<Source> Sources { get; } = new List<Source>();
}
