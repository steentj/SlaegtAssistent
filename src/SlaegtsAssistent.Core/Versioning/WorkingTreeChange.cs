namespace SlaegtsAssistent.Core.Versioning;

public enum WorkingTreeChangeKind
{
    Aendret,
    Ny,
    Slettet,
}

public sealed record WorkingTreeChange(string Path, WorkingTreeChangeKind Kind);
