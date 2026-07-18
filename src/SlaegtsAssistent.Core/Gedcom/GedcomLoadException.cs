namespace SlaegtsAssistent.Core.Gedcom;

public sealed class GedcomLoadException : Exception
{
    public GedcomLoadException(string message)
        : base(message)
    {
    }

    public GedcomLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
