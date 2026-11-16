namespace SlaegtsAssistent.Core.Versioning;

public enum DiffLineKind
{
    Context,
    Added,
    Removed,
    HunkHeader,
}

public sealed record DiffLine(DiffLineKind Kind, string Text);

public sealed record DiffFile(string Path, WorkingTreeChangeKind Kind, IReadOnlyList<DiffLine> Lines);
