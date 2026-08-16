using System.Text;
using System.Text.Json;

namespace SlaegtsAssistent.Core.Biography;

public static class BiographyDocumentSerializer
{
    public static string Serialize(BiographyDocumentMetadata metadata, string body)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(body);

        var builder = new StringBuilder()
            .AppendLine("---")
            .Append("formatVersion: ").AppendLine(metadata.FormatVersion.ToString())
            .Append("recordId: ").AppendLine(Serialize(metadata.RecordId))
            .Append("displayName: ").AppendLine(Serialize(metadata.DisplayName))
            .Append("gedcomBaselineHash: ").AppendLine(Serialize(metadata.GedcomBaselineHash))
            .Append("templateHash: ").AppendLine(Serialize(metadata.TemplateHash))
            .Append("syncBaseline: ").AppendLine(JsonSerializer.Serialize(metadata.SyncBaseline, BiographyDocumentJsonContext.Default.BiographySyncBaseline))
            .AppendLine("facts:")
            .Append("  fullName: ").AppendLine(Serialize(metadata.Facts.FullName))
            .Append("  sex: ").AppendLine(Serialize(metadata.Facts.Sex))
            .Append("  birthDate: ").AppendLine(Serialize(metadata.Facts.BirthDate))
            .Append("  birthPlace: ").AppendLine(Serialize(metadata.Facts.BirthPlace))
            .Append("  deathDate: ").AppendLine(Serialize(metadata.Facts.DeathDate))
            .Append("  deathPlace: ").AppendLine(Serialize(metadata.Facts.DeathPlace))
            .Append("  parentRecordIds: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.ParentRecordIds, BiographyDocumentJsonContext.Default.IReadOnlyListString))
            .AppendLine("---")
            .Append(body);

        return builder.ToString();
    }

    private static string Serialize(string? value) =>
        JsonSerializer.Serialize(value, BiographyDocumentJsonContext.Default.String);
}
