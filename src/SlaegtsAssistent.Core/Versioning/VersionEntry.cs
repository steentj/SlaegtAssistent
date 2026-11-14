namespace SlaegtsAssistent.Core.Versioning;

public sealed record VersionEntry(string Id, DateTimeOffset Time, string Message);

public sealed record VersionHistory(IReadOnlyList<VersionEntry> Entries, bool HasMore);
