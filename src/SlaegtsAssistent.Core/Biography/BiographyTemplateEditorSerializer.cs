using System.Text;
using System.Text.Json;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateEditorSerializer
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    public string Serialize(BiographyTemplateEditorModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ValidateVersion(model.Version);

        return JsonSerializer.Serialize(
                model,
                BiographyTemplateEditorJsonContext.Default.BiographyTemplateEditorModel)
            + "\n";
    }

    public BiographyTemplateEditorModel Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new BiographyTemplateEditorException("Skabelonfilen er tom.");
        }

        try
        {
            var model = JsonSerializer.Deserialize(
                    json,
                    BiographyTemplateEditorJsonContext.Default.BiographyTemplateEditorModel)
                ?? throw new BiographyTemplateEditorException("Skabelonfilen indeholder ingen model.");
            ValidateVersion(model.Version);
            return model;
        }
        catch (BiographyTemplateEditorException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new BiographyTemplateEditorException(
                $"Skabelonfilen kunne ikke læses: {exception.Message}",
                exception);
        }
    }

    public void Save(string filePath, BiographyTemplateEditorModel model)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Skabelonfilen er påkrævet.", nameof(filePath));
        }

        File.WriteAllText(filePath, Serialize(model), Utf8WithoutBom);
    }

    public BiographyTemplateEditorModel Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Skabelonfilen er påkrævet.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Skabelonfilen `{filePath}` blev ikke fundet.", filePath);
        }

        return Deserialize(File.ReadAllText(filePath, Utf8WithoutBom));
    }

    private static void ValidateVersion(int version)
    {
        if (version != BiographyTemplateEditorModel.CurrentVersion)
        {
            throw new BiographyTemplateEditorException(
                $"Skabelonversion {version} understøttes ikke. Den aktuelle version er {BiographyTemplateEditorModel.CurrentVersion}.");
        }
    }
}
