namespace SlaegtsAssistent.Core.Domain;

public sealed class Census
{
    public string? Date { get; set; }

    public string? Place { get; set; }

    internal GedcomNoteValue? NoteValue { get; set; }

    public string? Note
    {
        get => NoteValue?.Text;
        set => NoteValue = value is null ? null : new GedcomNoteValue(value);
    }

    public IList<Source> Sources { get; } = new List<Source>();
}
