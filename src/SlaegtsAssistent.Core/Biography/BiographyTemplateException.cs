namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateException : Exception
{
    public BiographyTemplateException(string message, string? filePath, int line, int column)
        : base($"{message} ({filePath ?? "indbygget skabelon"}, linje {line}, kolonne {column})")
    {
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    public string? FilePath { get; }

    public int Line { get; }

    public int Column { get; }
}
