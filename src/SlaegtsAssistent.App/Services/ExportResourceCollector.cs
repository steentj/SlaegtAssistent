using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SkiaSharp;

namespace SlaegtsAssistent.App.Services;

public sealed class ExportResourceCollector
{
    private const int MaximumImageBytes = 20 * 1024 * 1024;
    private const long MaximumTotalBytes = 100L * 1024 * 1024;
    // Stående A4 med 20 mm margen ved 96 billedpunkter pr. tomme.
    private const int A4ContentWidth = 642;
    private const int A4ContentHeight = 971;
    private readonly string _assetDirectory;
    private readonly IReadOnlyCollection<string> _roots;
    private readonly Dictionary<string, string> _assets = new(StringComparer.Ordinal);
    private long _totalBytes;

    public ExportResourceCollector(string assetDirectory, IReadOnlyCollection<string> allowedRoots)
    {
        _assetDirectory = Path.GetFullPath(assetDirectory);
        _roots = allowedRoots;
        Directory.CreateDirectory(_assetDirectory);
    }

    public string? Collect(string source, string documentPath, int widthPercent = 100, Action<string>? recordSourceHash = null)
    {
        if (widthPercent is not (25 or 50 or 100)) return null;
        try
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out _) || Path.IsPathRooted(source)) return null;
            var directory = Path.GetDirectoryName(Path.GetFullPath(documentPath))!;
            var path = SafeMarkdownPreviewService.ResolvePhysicalPath(Path.GetFullPath(Path.Combine(directory, Uri.UnescapeDataString(source.Split(['?', '#'], 2)[0]))));
            var roots = _roots.Append(directory).Select(SafeMarkdownPreviewService.ResolvePhysicalPath);
            if (!roots.Any(root => SafeMarkdownPreviewService.IsWithin(path, root))) return null;
            var file = new FileInfo(path);
            if (!file.Exists || file.Length <= 0 || file.Length > MaximumImageBytes) return null;
            using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = input.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (buffer.Length + read > MaximumImageBytes) return null;
                buffer.Write(chunk, 0, read);
            }
            var bytes = buffer.ToArray();
            if (!TryDimensions(bytes, out var width, out var height, out var extension) ||
                !(extension == ".png" ? Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)
                    : new[] { ".jpg", ".jpeg" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) ||
                width <= 0 || height <= 0 || (long)width * height > 40_000_000) return null;
            var sourceHash = Convert.ToHexString(SHA256.HashData(bytes));
            var maxWidth = A4ContentWidth * widthPercent / 100;
            var orientation = extension == ".jpg" ? JpegOrientation.Read(bytes) : 1;
            var displayedWidth = orientation >= 5 ? height : width;
            var displayedHeight = orientation >= 5 ? width : height;
            var heightLimitedWidth = Math.Max(1, (int)Math.Floor((double)displayedWidth * A4ContentHeight / displayedHeight));
            var targetWidth = Math.Min(displayedWidth, Math.Min(maxWidth, heightLimitedWidth));
            bytes = Resize(bytes, orientation, targetWidth);
            extension = ".png";
            if (bytes.Length > MaximumImageBytes) return null;
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (_assets.TryGetValue(hash, out var existing))
            {
                recordSourceHash?.Invoke(sourceHash);
                return "assets/" + existing;
            }
            if (_totalBytes + bytes.Length > MaximumTotalBytes) return null;
            var name = hash + extension;
            File.WriteAllBytes(Path.Combine(_assetDirectory, name), bytes);
            _totalBytes += bytes.Length;
            _assets.Add(hash, name);
            recordSourceHash?.Invoke(sourceHash);
            return "assets/" + name;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or UriFormatException or InvalidOperationException) { return null; }
    }

    private static byte[] Resize(byte[] data, int orientation, int maximumWidth)
    {
        using var original = SKBitmap.Decode(data) ?? throw new InvalidOperationException("Billedet kunne ikke dekodes.");
        var width = original.Width;
        var height = original.Height;
        var rotated = orientation >= 5;
        var displayedWidth = rotated ? height : width;
        var displayedHeight = rotated ? width : height;
        var scale = (double)maximumWidth / displayedWidth;
        var targetHeight = Math.Max(1, (int)Math.Round(displayedHeight * scale));
        using var target = new SKBitmap(maximumWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale((float)scale);
        var matrix = orientation switch
        {
            2 => Matrix(-1, 0, 0, 1, width, 0),
            3 => Matrix(-1, 0, 0, -1, width, height),
            4 => Matrix(1, 0, 0, -1, 0, height),
            5 => Matrix(0, 1, 1, 0, 0, 0),
            6 => Matrix(0, -1, 1, 0, height, 0),
            7 => Matrix(0, -1, -1, 0, height, width),
            8 => Matrix(0, 1, -1, 0, 0, width),
            _ => SKMatrix.Identity,
        };
        canvas.Concat(in matrix);
        using var image = SKImage.FromBitmap(original);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        canvas.Flush();
        using var encoded = target.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Billedet kunne ikke gemmes til eksport.");
        return AddPhysicalResolution(encoded.ToArray());
    }

    private static byte[] AddPhysicalResolution(byte[] png)
    {
        const int headerLength = 33;
        const int pixelsPerMetre = 3780;
        var result = new byte[png.Length + 21];
        png.AsSpan(0, headerLength).CopyTo(result);
        var chunk = result.AsSpan(headerLength, 21);
        BinaryPrimitives.WriteInt32BigEndian(chunk, 9);
        "pHYs"u8.CopyTo(chunk[4..]);
        BinaryPrimitives.WriteInt32BigEndian(chunk[8..], pixelsPerMetre);
        BinaryPrimitives.WriteInt32BigEndian(chunk[12..], pixelsPerMetre);
        chunk[16] = 1;
        uint crc = uint.MaxValue;
        foreach (var value in chunk.Slice(4, 13))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0u);
        }
        BinaryPrimitives.WriteUInt32BigEndian(chunk[17..], ~crc);
        png.AsSpan(headerLength).CopyTo(result.AsSpan(headerLength + chunk.Length));
        return result;
    }

    private static SKMatrix Matrix(float scaleX, float skewX, float skewY, float scaleY, float translateX, float translateY) =>
        new() { ScaleX = scaleX, SkewX = skewX, SkewY = skewY, ScaleY = scaleY,
            TransX = translateX, TransY = translateY, Persp2 = 1 };

    internal static bool TryDimensions(byte[] bytes, out int width, out int height, out string extension)
    {
        width = height = 0; extension = "";
        if (bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8))
        {
            width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            extension = ".png"; return true;
        }
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8) return false;
        for (var i = 2; i + 3 < bytes.Length;)
        {
            if (bytes[i++] != 0xff) return false;
            while (i < bytes.Length && bytes[i] == 0xff) i++;
            if (i >= bytes.Length) return false;
            var marker = bytes[i++];
            if (marker is 0xd8 or 0x01 || marker is >= 0xd0 and <= 0xd7) continue;
            if (marker is 0xd9 or 0xda || i + 1 >= bytes.Length) return false;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i, 2));
            if (length < 2 || i + length > bytes.Length) return false;
            if (marker is >= 0xc0 and <= 0xcf && marker is not (0xc4 or 0xc8 or 0xcc))
            {
                if (length < 7) return false;
                height = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i + 3, 2));
                width = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i + 5, 2));
                extension = ".jpg"; return true;
            }
            i += length;
        }
        return false;
    }
}
