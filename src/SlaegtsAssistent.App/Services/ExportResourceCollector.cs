using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SlaegtsAssistent.App.Services;

public sealed class ExportResourceCollector
{
    private const int MaximumImageBytes = 20 * 1024 * 1024;
    private const long MaximumTotalBytes = 100L * 1024 * 1024;
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

    public string? Collect(string source, string documentPath)
    {
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
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (_assets.TryGetValue(hash, out var existing)) return "assets/" + existing;
            if (_totalBytes + bytes.Length > MaximumTotalBytes) return null;
            var name = hash + extension;
            File.WriteAllBytes(Path.Combine(_assetDirectory, name), bytes);
            _totalBytes += bytes.Length;
            _assets.Add(hash, name);
            return "assets/" + name;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or UriFormatException) { return null; }
    }

    private static bool TryDimensions(byte[] bytes, out int width, out int height, out string extension)
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
