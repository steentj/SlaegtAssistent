using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Gedcom;

public sealed class GedcomLoadException : Exception
{
    public GedcomLoadException(string message)
        : base(message)
    {
    }

    public GedcomLoadException(string message, GedcomImportReport importReport)
        : base(message)
    {
        ImportReport = importReport;
    }

    public GedcomLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public GedcomLoadException(
        string message,
        Exception innerException,
        GedcomImportReport importReport)
        : base(message, innerException)
    {
        ImportReport = importReport;
    }

    public GedcomImportReport? ImportReport { get; }
}
