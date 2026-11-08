namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateEditorException : Exception
{
    public BiographyTemplateEditorException(string message)
        : base(message)
    {
    }

    public BiographyTemplateEditorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
