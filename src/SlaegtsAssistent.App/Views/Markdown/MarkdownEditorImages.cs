using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using SkiaSharp;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Views.Markdown;

internal sealed class MarkdownEditorImages(Action imageChanged)
{
    private const int MaximumDimension = 1200;
    private readonly Dictionary<string, ImageEntry> _images = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _loadSlots = new(2);
    private CancellationTokenSource? _loading;
    private string? _directory;
    private string[] _roots = [];

    public void SetDocument(string? path, IReadOnlyCollection<string> roots)
    {
        var (directory, allowedRoots) = path is null
            ? (null, Array.Empty<string>())
            : SafeMarkdownPreviewService.GetLocalImageRoots(path, roots);
        if (_directory == directory && _roots.SequenceEqual(allowedRoots)) return;
        Clear();
        _directory = directory;
        _roots = allowedRoots;
    }

    public Bitmap? Find(string source)
    {
        if (_directory is null) return null;
        if (_images.TryGetValue(source, out var cached)) return cached.Bitmap;

        var entry = new ImageEntry();
        _images.Add(source, entry);
        _loading ??= new CancellationTokenSource();
        _ = LoadAsync(source, entry, _directory, _roots, _loading.Token);
        return null;
    }

    public void Retain(IEnumerable<string> sources)
    {
        var inUse = sources.ToHashSet(StringComparer.Ordinal);
        foreach (var source in _images.Keys.Where(source => !inUse.Contains(source)).ToArray())
        {
            _images[source].Bitmap?.Dispose();
            _images.Remove(source);
        }
    }

    public void RetryFailures()
    {
        foreach (var source in _images.Keys.Where(source => _images[source] is { Finished: true, Bitmap: null }).ToArray())
            _images.Remove(source);
    }

    public void Clear()
    {
        _loading?.Cancel();
        _loading?.Dispose();
        _loading = null;
        Retain([]);
    }

    private async Task LoadAsync(string source, ImageEntry entry, string directory, string[] roots, CancellationToken token)
    {
        Bitmap? bitmap;
        try
        {
            await _loadSlots.WaitAsync(token);
            try
            {
                if (!_images.TryGetValue(source, out var pending) || pending != entry) return;
                bitmap = await Task.Run(() => Decode(source, directory, roots, token), token);
            }
            finally
            {
                _loadSlots.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested || !_images.TryGetValue(source, out var current) || current != entry)
        {
            bitmap?.Dispose();
            return;
        }

        entry.Bitmap = bitmap;
        entry.Finished = true;
        if (bitmap is not null) imageChanged();
    }

    private static Bitmap? Decode(string source, string directory, string[] roots, CancellationToken token)
    {
        if (SafeMarkdownPreviewService.ReadLocalImage(source, directory, roots).Bytes is not { Length: > 0 } data)
            return null;
        token.ThrowIfCancellationRequested();
        try
        {
            using var stream = new MemoryStream(data, writable: false);
            using var inspection = new MemoryStream(data, writable: false);
            using var decoder = SKCodec.Create(inspection);
            if (decoder is null || decoder.Info.Width <= 0 || decoder.Info.Height <= 0 ||
                (long)decoder.Info.Width * decoder.Info.Height > 40_000_000)
                return null;

            var size = decoder.Info;
            if (size.Width > MaximumDimension && size.Width >= size.Height)
                return Bitmap.DecodeToWidth(stream, MaximumDimension);
            if (size.Height > MaximumDimension)
                return Bitmap.DecodeToHeight(stream, MaximumDimension);
            return new Bitmap(stream);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed class ImageEntry
    {
        public bool Finished { get; set; }
        public Bitmap? Bitmap { get; set; }
    }
}
