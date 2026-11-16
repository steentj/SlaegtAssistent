using System.Threading;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed record MediaInspection(bool CanSelect, string Status, string? RelativePath, string? ResolvedPath = null, string? ThumbnailKey = null);

public interface IMediaInspectionService
{
    Task<MediaInspection> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken);
}
