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
            return MoveLegacyNotes(model);
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

    private static BiographyTemplateEditorModel MoveLegacyNotes(BiographyTemplateEditorModel model)
    {
        if (model.Blocks.Any(block => block is null))
        {
            return model;
        }

        var facts = model.Blocks.FirstOrDefault(block => block.Type == BiographyTemplateBlockType.Facts);
        var noteFields = facts?.Fields.Where(field =>
            field is BiographyTemplateField.PersonNote or BiographyTemplateField.FamilyNote).ToArray() ?? [];
        if (facts is null || noteFields.Length == 0)
        {
            return model;
        }

        var blocks = model.Blocks.ToList();
        var factsIndex = blocks.IndexOf(facts);
        blocks[factsIndex] = new BiographyTemplateBlock(facts.Type, facts.IsVisible, facts.Heading,
            facts.Presentation, facts.Fields.Except(noteFields).ToArray());
        var existingIndex = blocks.FindIndex(block => block.Type == BiographyTemplateBlockType.Notes);
        if (existingIndex < 0)
        {
            blocks.Insert(factsIndex + 1, new BiographyTemplateBlock(
                BiographyTemplateBlockType.Notes, facts.IsVisible, "Notater", facts.Presentation, noteFields));
        }
        else
        {
            var existing = blocks[existingIndex];
            blocks[existingIndex] = new BiographyTemplateBlock(existing.Type, existing.IsVisible, existing.Heading,
                existing.Presentation, existing.Fields.Concat(noteFields).Distinct().ToArray());
        }

        return new BiographyTemplateEditorModel(model.Version, blocks);
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
