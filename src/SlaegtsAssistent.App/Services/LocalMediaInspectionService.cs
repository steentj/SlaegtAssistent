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
    private static readonly SemaphoreSlim Slots = new(2);

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

    private Task<MediaInspection> InspectBoundedAsync(
        MediaCatalogItem item, int maximumWidth, int maximumHeight, CancellationToken cancellationToken) =>
        ReadImageAsync(item, new PixelSize(maximumWidth, maximumHeight), (key, bitmap) =>
        {
            if (thumbnailCache is null) bitmap.Dispose();
            else thumbnailCache.Store(key, bitmap);
        }, cancellationToken);

    internal async Task<MediaInspection> ReadImageAsync(
        MediaCatalogItem item,
        PixelSize? maximumSize,
        Action<string, Bitmap>? receiveBitmap,
        CancellationToken cancellationToken,
        Func<string, bool>? hasCachedImage = null)
    {
        await Slots.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(
                () => Inspect(item, maximumSize, receiveBitmap, hasCachedImage, cancellationToken), cancellationToken);
        }
        finally { Slots.Release(); }
    }

    internal static bool IsSupportedImage(string? reference) =>
        Path.GetExtension(reference) is { } extension &&
        (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
         extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
         extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase));

    private MediaInspection Inspect(
        MediaCatalogItem item, PixelSize? maximumSize, Action<string, Bitmap>? receiveBitmap,
        Func<string, bool>? hasCachedImage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolution = new BiographyMediaResolver().Resolve(item.Reference, gedcomDirectory, documentDirectory, mediaDirectory, Volatile.Read(ref _index));
        if (resolution.RelativePath is null || resolution.ResolvedPath is null)
            return new(false, resolution.Diagnostic ?? "Filen blev ikke fundet.", null);
        var extension = Path.GetExtension(resolution.ResolvedPath);
        if (!IsSupportedImage(resolution.ResolvedPath))
            return new(false, "Kun PNG- og JPEG-billeder kan vises.", null, resolution.ResolvedPath);
        try
        {
            var physicalPath = SafeMarkdownPreviewService.ResolvePhysicalPath(resolution.ResolvedPath);
            var roots = new[] { documentDirectory, gedcomDirectory, mediaDirectory };
            if (!Array.Exists(roots, root => !string.IsNullOrWhiteSpace(root) &&
                SafeMarkdownPreviewService.IsWithin(physicalPath, SafeMarkdownPreviewService.ResolvePhysicalPath(root))))
                return new(false, "Billedet ligger uden for de tilladte lokale mapper.", null);
            var information = new FileInfo(physicalPath);
            var lastWrite = information.LastWriteTimeUtc;
            using var input = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var length = input.Length;
            if (length <= 0 || length > MaximumBytes)
                return new(false, "Filen er tom eller større end 20 MB.", null, resolution.ResolvedPath);
            var key = $"{physicalPath}\0{length}\0{lastWrite.Ticks}\0{maximumSize?.Width}x{maximumSize?.Height}";
            var success = new MediaInspection(true, "Klar til valg", resolution.RelativePath, resolution.ResolvedPath, key);
            if (receiveBitmap is null || hasCachedImage?.Invoke(key) == true) return success;

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
            data.Position = 0;
            try
            {
                Bitmap? bitmap = ApplyOrientation(Decode(data, width, height, orientation, maximumSize), orientation);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    information.Refresh();
                    if (!information.Exists || information.Length != length || information.LastWriteTimeUtc != lastWrite)
                        return new(false, "Mediefilen blev ændret under indlæsningen.", null, resolution.ResolvedPath);
                    var result = bitmap;
                    bitmap = null;
                    receiveBitmap(key, result);
                    return success;
                }
                finally { bitmap?.Dispose(); }
            }
            catch (NullReferenceException)
            {
                return new(false, "Billedet er beskadiget og kan ikke dekodes.", null, resolution.ResolvedPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return new(false, "Filen kan ikke læses eller dekodes.", null, resolution.ResolvedPath);
        }
    }
    private static Bitmap Decode(Stream data, int width, int height, int orientation, PixelSize? maximumSize)
    {
        if (maximumSize is not { } size) return new Bitmap(data);
        var transposed = orientation >= 5;
        var maxWidth = transposed ? size.Height : size.Width;
        var maxHeight = transposed ? size.Width : size.Height;
        return (double)width / maxWidth > (double)height / maxHeight
            ? Bitmap.DecodeToWidth(data, Math.Min(maxWidth, width))
            : Bitmap.DecodeToHeight(data, Math.Min(maxHeight, height));
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
