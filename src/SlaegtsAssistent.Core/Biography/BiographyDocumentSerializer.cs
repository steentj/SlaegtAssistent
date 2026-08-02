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
            .Append("recordId: ").AppendLine(JsonSerializer.Serialize(metadata.RecordId))
            .Append("displayName: ").AppendLine(JsonSerializer.Serialize(metadata.DisplayName))
            .Append("gedcomBaselineHash: ").AppendLine(JsonSerializer.Serialize(metadata.GedcomBaselineHash))
            .AppendLine("facts:")
            .Append("  fullName: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.FullName))
            .Append("  sex: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.Sex))
            .Append("  birthDate: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.BirthDate))
            .Append("  birthPlace: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.BirthPlace))
            .Append("  deathDate: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.DeathDate))
            .Append("  deathPlace: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.DeathPlace))
            .Append("  parentRecordIds: ").AppendLine(JsonSerializer.Serialize(metadata.Facts.ParentRecordIds))
            .AppendLine("---")
            .Append(body);

        return builder.ToString();
    }
}
