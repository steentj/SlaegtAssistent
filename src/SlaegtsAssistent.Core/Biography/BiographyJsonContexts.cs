using System.Text.Json.Serialization;

namespace SlaegtsAssistent.Core.Biography;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CanonicalBiographySnapshot))]
internal partial class CanonicalBiographyJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(BiographySyncBaseline))]
[JsonSerializable(typeof(CanonicalPersonData))]
[JsonSerializable(typeof(CanonicalFamilyData))]
[JsonSerializable(typeof(CanonicalEventData))]
[JsonSerializable(typeof(CanonicalCensusData))]
[JsonSerializable(typeof(CanonicalSourceData))]
[JsonSerializable(typeof(CanonicalMediaData))]
[JsonSerializable(typeof(CanonicalSubmitterData))]
internal partial class BiographyDocumentJsonContext : JsonSerializerContext;
