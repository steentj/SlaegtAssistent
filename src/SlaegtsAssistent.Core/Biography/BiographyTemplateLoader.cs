using System.Text;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateLoader
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly BiographyTemplateParser _parser = new();

    public BiographyTemplate Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Skabelonfilen er påkrævet.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Skabelonfilen blev ikke fundet.", filePath);
        }

        return _parser.Parse(File.ReadAllText(filePath, Utf8WithoutBom), filePath);
    }

    public BiographyTemplate Parse(string source, string? filePath = null)
    {
        return _parser.Parse(source, filePath);
    }
}
