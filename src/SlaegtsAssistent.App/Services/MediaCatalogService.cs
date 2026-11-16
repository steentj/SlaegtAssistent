using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed record MediaCatalogSource(string OwnerId, BiographyDocument? Document, CanonicalBiographySnapshot? ApprovedFallback);
public sealed record MediaCatalogItem(string Key, string OwnerId, string? RecordId, string? Title, string? Reference)
{
    public string FileName => (Reference ?? string.Empty).Replace('\\', '/').Split('/').LastOrDefault() ?? string.Empty;
}

public interface IMediaCatalogService
{
    IReadOnlyList<MediaCatalogItem> Build(IEnumerable<MediaCatalogSource> sources);
}

public sealed class MediaCatalogService : IMediaCatalogService
{
    public IReadOnlyList<MediaCatalogItem> Build(IEnumerable<MediaCatalogSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var items = new List<MediaCatalogItem>();
        foreach (var source in sources)
        {
            var baseline = source.Document?.Metadata?.SyncBaseline;
            var snapshot = baseline is { Version: BiographySyncBaseline.CurrentVersion } &&
                           baseline.Approved.Version == CanonicalBiographySnapshot.CurrentVersion
                ? baseline.Approved
                : source.ApprovedFallback;
            if (snapshot is null || snapshot.Version != CanonicalBiographySnapshot.CurrentVersion ||
                !string.Equals(snapshot.Person.RecordId, source.OwnerId, StringComparison.Ordinal)) continue;
            foreach (var media in snapshot.Person.Media)
            {
                var identity = string.IsNullOrWhiteSpace(media.RecordId)
                    ? "inline:" + Hash(source.OwnerId + "\0" + (media.File ?? string.Empty))
                    : "id:" + media.RecordId;
                items.Add(new MediaCatalogItem(source.OwnerId + "|" + identity, source.OwnerId,
                    media.RecordId, media.Title, media.File));
            }
        }
        return items.DistinctBy(item => item.Key, StringComparer.Ordinal).ToArray();
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

public sealed record MediaSelectionRequest(string PersonId, string DocumentPath, long BufferRevision,
    long MediaContextRevision, bool IsPortrait);
public sealed record MediaSelectionResult(string Reference, string AlternativeText, string? Caption,
    BiographyImageWidth Width = BiographyImageWidth.FullPage);
