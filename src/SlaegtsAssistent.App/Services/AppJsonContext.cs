using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SlaegtsAssistent.App.Services;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(GedcomSnapshotManifest))]
internal partial class AppJsonContext : JsonSerializerContext;

internal sealed record GedcomSnapshotManifest(
    int FormatVersion,
    string SourcePath,
    string SourceFileName,
    string SourceHash,
    DateTimeOffset ImportedAt,
    string SourceCopyFileName,
    IReadOnlyDictionary<string, string> RawPersonSegments);
