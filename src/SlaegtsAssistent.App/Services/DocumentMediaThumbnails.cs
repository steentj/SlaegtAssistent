using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;

namespace SlaegtsAssistent.App.Services;

internal sealed class DocumentMediaThumbnails
{
    private const int MaximumCacheCharacters = 16 * 1024 * 1024;
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _cache = new(StringComparer.Ordinal);
    private readonly Queue<string> _insertionOrder = new();
    private int _cachedCharacters;

    public async Task<DocumentMediaCard> InspectAsync(
        DocumentMediaCard card, LocalMediaInspectionService inspection, bool showThumbnail, CancellationToken cancellationToken)
    {
        if (card.Navigation is null) return card;

        string? thumbnail = null;
        Action<string, Bitmap>? encode = showThumbnail
            ? (key, bitmap) =>
            {
                using (bitmap)
                using (var encoded = new MemoryStream())
                {
                    bitmap.Save(encoded, new PngBitmapEncoderOptions());
                    thumbnail = Convert.ToBase64String(encoded.GetBuffer(), 0, (int)encoded.Length);
                }
                Store(key, thumbnail);
            }
            : null;
        var result = await inspection.ReadImageAsync(card.Item, new PixelSize(160, 120), encode,
            cancellationToken, showThumbnail ? TryGetCached : null);
        return result.CanSelect ? card with { Thumbnail = thumbnail }
            : card with { Navigation = null, Status = result.Status };

        bool TryGetCached(string key)
        {
            lock (_gate) return _cache.TryGetValue(key, out thumbnail);
        }
    }

    private void Store(string key, string thumbnail)
    {
        lock (_gate)
        {
            if (thumbnail.Length > MaximumCacheCharacters || _cache.ContainsKey(key)) return;
            while (_cachedCharacters + thumbnail.Length > MaximumCacheCharacters)
            {
                var oldest = _insertionOrder.Dequeue();
                _cachedCharacters -= _cache[oldest].Length;
                _cache.Remove(oldest);
            }
            _cache.Add(key, thumbnail);
            _insertionOrder.Enqueue(key);
            _cachedCharacters += thumbnail.Length;
        }
    }
}
