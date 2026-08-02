using System;
using System.Collections.Generic;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public sealed record GedcomSnapshot(
    int FormatVersion,
    string SourcePath,
    string SourceFileName,
    string SourceHash,
    DateTimeOffset ImportedAt,
    IReadOnlyDictionary<string, string> RawPersonSegments);

public interface IGedcomSnapshotStore
{
    GedcomSnapshot? Load(string? outputDirectory);

    void Save(string outputDirectory, string sourcePath, FamilyTree familyTree);
}
