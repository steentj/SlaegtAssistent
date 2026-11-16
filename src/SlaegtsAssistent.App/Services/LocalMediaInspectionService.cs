using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed class LocalMediaInspectionService(
    string documentDirectory,
    string? gedcomDirectory,
    string? mediaDirectory,
    MediaFileIndex? index,
    MediaThumbnailCache? thumbnailCache = null) : IMediaInspectionService
{
    private const int MaximumBytes = 20 * 1024 * 1024;
    private MediaFileIndex? _index = index;
    private readonly SemaphoreSlim _slots = new(2);

    public void SetIndex(MediaFileIndex? index) => Volatile.Write(ref _index, index);

    public async Task RebuildIndexAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mediaDirectory)) return;
        var updated = await MediaFileIndex.BuildAsync(mediaDirectory,
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Volatile.Write(ref _index, updated);
    }

    public Task<MediaInspection> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken) =>
        InspectBoundedAsync(item, 160, 120, cancellationToken);

    public Task<MediaInspection> InspectPreviewAsync(MediaCatalogItem item, CancellationToken cancellationToken) =>
        InspectBoundedAsync(item, 480, 360, cancellationToken);

    private async Task<MediaInspection> InspectBoundedAsync(MediaCatalogItem item, int maximumWidth, int maximumHeight, CancellationToken cancellationToken)
    {
        await _slots.WaitAsync(cancellationToken);
        try { return await Task.Run(() => Inspect(item, maximumWidth, maximumHeight, cancellationToken), cancellationToken); }
        finally { _slots.Release(); }
    }

    private MediaInspection Inspect(MediaCatalogItem item, int maximumWidth, int maximumHeight, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolution = new BiographyMediaResolver().Resolve(item.Reference, gedcomDirectory, documentDirectory, mediaDirectory, Volatile.Read(ref _index));
        if (resolution.RelativePath is null || resolution.ResolvedPath is null)
            return new(false, resolution.Diagnostic ?? "Filen blev ikke fundet.", null);
        var extension = Path.GetExtension(resolution.ResolvedPath);
        if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return new(false, "Kun PNG- og JPEG-billeder kan vælges.", null, resolution.ResolvedPath);
        try
        {
            var information = new FileInfo(resolution.ResolvedPath);
            var lastWrite = information.LastWriteTimeUtc;
            using var input = new FileStream(resolution.ResolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var length = input.Length;
            if (input.Length <= 0 || input.Length > MaximumBytes)
                return new(false, "Filen er tom eller større end 20 MB.", null, resolution.ResolvedPath);
            using var data = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (data.Length + read > MaximumBytes) return new(false, "Filen er større end 20 MB.", null, resolution.ResolvedPath);
                data.Write(buffer, 0, read);
            }
            var bytes = data.ToArray();
            if (!ExportResourceCollector.TryDimensions(bytes, out var width, out var height, out var format) ||
                width <= 0 || height <= 0 || (long)width * height > 40_000_000 ||
                (format == ".png") != extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                return new(false, "Billedet har ugyldigt format eller er større end 40 millioner pixels.", null, resolution.ResolvedPath);
            var orientation = format == ".jpg" ? JpegOrientation.Read(bytes) : 1;
            var transposed = orientation >= 5;
            var maxWidth = transposed ? maximumHeight : maximumWidth;
            var maxHeight = transposed ? maximumWidth : maximumHeight;
            data.Position = 0;
            try
            {
                var bitmap = (double)width / maxWidth > (double)height / maxHeight
                    ? Bitmap.DecodeToWidth(data, Math.Min(maxWidth, width))
                    : Bitmap.DecodeToHeight(data, Math.Min(maxHeight, height));
                bitmap = ApplyOrientation(bitmap, orientation);
                if (cancellationToken.IsCancellationRequested)
                {
                    bitmap.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                information.Refresh();
                if (!information.Exists || information.Length != length || information.LastWriteTimeUtc != lastWrite)
                {
                    bitmap.Dispose();
                    return new(false, "Filen er ændret. Opdatér billedvælgeren.", null, resolution.ResolvedPath);
                }
                var key = $"{resolution.ResolvedPath}\0{length}\0{lastWrite.Ticks}\0{maximumWidth}x{maximumHeight}";
                if (thumbnailCache is null) bitmap.Dispose();
                else thumbnailCache.Store(key, bitmap);
                return new(true, "Klar til valg", resolution.RelativePath, resolution.ResolvedPath, key);
            }
            catch (NullReferenceException)
            {
                return new(false, "Billedet er beskadiget og kan ikke dekodes.", null, resolution.ResolvedPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return new(false, "Filen kan ikke læses eller dekodes. Vælg et andet billede.", null, resolution.ResolvedPath);
        }
    }
    private static Bitmap ApplyOrientation(Bitmap bitmap, int orientation)
    {
        if (orientation == 1) return bitmap;
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        var transposed = orientation >= 5;
        var target = new RenderTargetBitmap(new PixelSize(transposed ? height : width, transposed ? width : height));
        var matrix = orientation switch
        {
            2 => new Matrix(-1, 0, 0, 1, width, 0),
            3 => new Matrix(-1, 0, 0, -1, width, height),
            4 => new Matrix(1, 0, 0, -1, 0, height),
            5 => new Matrix(0, 1, 1, 0, 0, 0),
            6 => new Matrix(0, 1, -1, 0, height, 0),
            7 => new Matrix(0, -1, -1, 0, height, width),
            8 => new Matrix(0, -1, 1, 0, 0, width),
            _ => Matrix.Identity,
        };
        try
        {
            using var context = target.CreateDrawingContext();
            using (context.PushTransform(matrix)) context.DrawImage(bitmap, new Rect(0, 0, width, height));
            return target;
        }
        catch { target.Dispose(); throw; }
        finally { bitmap.Dispose(); }
    }

}
