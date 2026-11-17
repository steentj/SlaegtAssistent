using System.Collections;

namespace SlaegtsAssistent.Core.Domain;

internal sealed class SharedGedcomNote(string text)
{
    public string Text { get; set; } = text;
    public string? Timestamp { get; set; }
}

internal sealed record GedcomNotePart(string Text, string? Timestamp);

internal sealed class GedcomNoteValue
{
    private readonly string? _text;
    private readonly SharedGedcomNote? _shared;
    private readonly IReadOnlyList<GedcomNoteValue>? _parts;

    public GedcomNoteValue(string text) => _text = text;

    public GedcomNoteValue(SharedGedcomNote shared) => _shared = shared;

    public GedcomNoteValue(IReadOnlyList<GedcomNoteValue> parts) => _parts = parts;

    public string Text => _parts is not null
        ? string.Join("\n\n", _parts.Select(part => part.Text))
        : _shared?.Text ?? _text!;

    public IEnumerable<GedcomNotePart> Parts => _parts is not null
        ? _parts.SelectMany(part => part.Parts)
        : [new GedcomNotePart(Text, _shared?.Timestamp)];
}

internal sealed class GedcomNoteCollection : IList<string>
{
    private readonly List<GedcomNoteValue> _notes = [];

    public string this[int index]
    {
        get => _notes[index].Text;
        set => _notes[index] = new GedcomNoteValue(value);
    }

    public int Count => _notes.Count;
    public bool IsReadOnly => false;

    public void Add(string item) => _notes.Add(new GedcomNoteValue(item));
    internal void Add(GedcomNoteValue item) => _notes.Add(item);
    internal IReadOnlyList<GedcomNoteValue> Values => _notes;
    public void Clear() => _notes.Clear();
    public bool Contains(string item) => IndexOf(item) >= 0;
    public void CopyTo(string[] array, int arrayIndex) =>
        _notes.Select(note => note.Text).ToArray().CopyTo(array, arrayIndex);
    public IEnumerator<string> GetEnumerator() => _notes.Select(note => note.Text).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int IndexOf(string item) => _notes.FindIndex(note => note.Text == item);
    public void Insert(int index, string item) => _notes.Insert(index, new GedcomNoteValue(item));
    public bool Remove(string item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        _notes.RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index) => _notes.RemoveAt(index);
}
