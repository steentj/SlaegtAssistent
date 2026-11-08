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
            throw new FileNotFoundException(
                $"Skabelonfilen `{filePath}` blev ikke fundet.",
                filePath);
        }

        var model = LoadEditorModel(filePath);
        return _parser.Parse(new BiographyTemplateEditorCompiler().Compile(model), filePath);
    }

    public BiographyTemplateEditorModel LoadEditorModel(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var source = File.ReadAllText(filePath, Utf8WithoutBom);
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new BiographyTemplateException("Skabelonfilen er tom.", filePath, 1, 1);
        }
        if (!source.TrimStart().StartsWith('{'))
        {
            throw new BiographyTemplateException(
                "Det tidligere skabelonformat understøttes ikke. Åbn skabeloneditoren for at oprette en ny skabelon, eller vælg standardskabelonen.",
                filePath, 1, 1);
        }

        try
        {
            var model = new BiographyTemplateEditorSerializer().Deserialize(source);
            _ = new BiographyTemplateEditorCompiler().Compile(model);
            return model;
        }
        catch (Exception exception) when (exception is BiographyTemplateEditorException or ArgumentException)
        {
            throw new BiographyTemplateException(exception.Message, filePath, 1, 1);
        }
    }

    public BiographyTemplate Parse(string source, string? filePath = null)
    {
        return _parser.Parse(source, filePath);
    }
}
