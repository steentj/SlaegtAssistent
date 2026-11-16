using Avalonia;
using Avalonia.Media.Imaging;
using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class MediaThumbnailCacheTests
{
    [Fact]
    public Task CachenHolderBudgetOgFjernerMindstBrugteMiniature() => HeadlessTestApplication.RunAsync(() =>
    {
        using var cache = new MediaThumbnailCache(800);
        cache.Store("a", new RenderTargetBitmap(new PixelSize(10, 10))).Should().BeTrue();
        cache.Store("b", new RenderTargetBitmap(new PixelSize(10, 10))).Should().BeTrue();
        cache.Find("a").Should().NotBeNull();
        cache.Store("c", new RenderTargetBitmap(new PixelSize(10, 10))).Should().BeTrue();
        cache.Find("b").Should().BeNull();
        cache.Find("a").Should().NotBeNull();
        cache.Find("c").Should().NotBeNull();
        cache.Clear();
        cache.Find("a").Should().BeNull();
        return Task.CompletedTask;
    });
}
