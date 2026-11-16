using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace SlaegtsAssistent.App.Services;

public sealed class MediaThumbnailCache : IDisposable
{
    public const long MaximumBytes = 64L * 1024 * 1024;
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _items = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _recent = new();
    private readonly long _limit;
    private long _bytes;
    private bool _disposed;

    public MediaThumbnailCache(long maximumBytes = MaximumBytes)
    {
        if (maximumBytes <= 0 || maximumBytes > MaximumBytes) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        _limit = maximumBytes;
    }

    public bool Store(string key, Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        lock (_gate)
        {
            var size = (long)bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4;
            if (_disposed || size <= 0 || size > _limit || _items.ContainsKey(key))
            {
                bitmap.Dispose();
                return false;
            }
            while (_bytes + size > _limit)
            {
                var oldest = _recent.Last!;
                _recent.RemoveLast();
                _items.Remove(oldest.Value.Key);
                _bytes -= oldest.Value.Size;
                oldest.Value.Bitmap.Dispose();
            }
            var node = _recent.AddFirst(new Entry(key, bitmap, size));
            _items.Add(key, node);
            _bytes += size;
            return true;
        }
    }

    public Bitmap? Find(string key)
    {
        lock (_gate)
        {
            if (_disposed || !_items.TryGetValue(key, out var node)) return null;
            _recent.Remove(node);
            _recent.AddFirst(node);
            return node.Value.Bitmap;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            foreach (var item in _recent) item.Bitmap.Dispose();
            _items.Clear();
            _recent.Clear();
            _bytes = 0;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }
    }

    private sealed record Entry(string Key, Bitmap Bitmap, long Size);
}
