using System.Text.Json.Serialization;

namespace SlaegtsAssistent.Core.Biography;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(BiographyTemplateEditorModel))]
internal partial class BiographyTemplateEditorJsonContext : JsonSerializerContext;
