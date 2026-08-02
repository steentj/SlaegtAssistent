using System;

namespace SlaegtsAssistent.App.Services;

public sealed class GedcomSnapshotException : Exception
{
    public GedcomSnapshotException(string message)
        : base(message)
    {
    }

    public GedcomSnapshotException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
