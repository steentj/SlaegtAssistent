using System.Buffers.Binary;
using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class LocalMediaInspectionServiceTests
{
    [Fact]
    public async Task StoerreForhaandsvisningHarEgenCachepost()
    {
        await HeadlessTestApplication.RunAsync(async () =>
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllBytes(Path.Combine(root, "foto.png"), Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC"));
                using var cache = new MediaThumbnailCache();
                var service = new LocalMediaInspectionService(root, root, null, null, cache);
                var item = new MediaCatalogItem("a", "a", null, null, "foto.png");
                var thumbnail = await service.InspectAsync(item, CancellationToken.None);
                var preview = await service.InspectPreviewAsync(item, CancellationToken.None);
                thumbnail.ThumbnailKey.Should().NotBe(preview.ThumbnailKey);
                cache.Find(preview.ThumbnailKey!)!.PixelSize.Width.Should().BeGreaterThan(cache.Find(thumbnail.ThumbnailKey!)!.PixelSize.Width);
            }
            finally { Directory.Delete(root, true); }
        });
    }

    [Fact]
    public async Task MiniatureRespektererExifOrientering()
    {
        await HeadlessTestApplication.RunAsync(async () =>
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllBytes(Path.Combine(root, "drejet.jpg"), Convert.FromBase64String("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"));
                using var cache = new MediaThumbnailCache();
                var service = new LocalMediaInspectionService(root, root, null, null, cache);
                var result = await service.InspectAsync(new("a", "a", null, null, "drejet.jpg"), CancellationToken.None);
                result.CanSelect.Should().BeTrue();
                var bitmap = cache.Find(result.ThumbnailKey!);
                bitmap.Should().NotBeNull();
                bitmap!.PixelSize.Width.Should().BeLessThan(bitmap.PixelSize.Height);
                bitmap.PixelSize.Height.Should().BeLessThanOrEqualTo(120);
            }
            finally { Directory.Delete(root, true); }
        });
    }

    [Fact]
    public async Task GenindekseringFinderNyFlyttetFil()
    {
        await HeadlessTestApplication.RunAsync(async () =>
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var media = Directory.CreateDirectory(Path.Combine(root, "medier")).FullName;
            var docs = Directory.CreateDirectory(Path.Combine(root, "dokumenter")).FullName;
            try
            {
                var index = await SlaegtsAssistent.Core.Biography.MediaFileIndex.BuildAsync(media, StringComparer.Ordinal);
                var service = new LocalMediaInspectionService(docs, null, media, index);
                var item = new MediaCatalogItem("a", "a", null, null, "tidligere/foto.png");
                (await service.InspectAsync(item, CancellationToken.None)).CanSelect.Should().BeFalse();
                var nested = Directory.CreateDirectory(Path.Combine(media, "flyttet", "tidligere")).FullName;
                var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC");
                File.WriteAllBytes(Path.Combine(nested, "foto.png"), png);
                await service.RebuildIndexAsync(CancellationToken.None);
                (await service.InspectAsync(item, CancellationToken.None)).CanSelect.Should().BeTrue();
            }
            finally { Directory.Delete(root, true); }
        });
    }

    [Fact]
    public async Task KunReellePngOgJpegMedGyldigDekodningKanVaelges()
    {
        await HeadlessTestApplication.RunAsync(async () =>
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC");
                File.WriteAllBytes(Path.Combine(root, "foto.png"), png);
                File.WriteAllText(Path.Combine(root, "falsk.png"), "ikke et billede");
                File.WriteAllBytes(Path.Combine(root, "beskadiget.png"), png[..24]);
                var enormous = png[..24];
                BinaryPrimitives.WriteInt32BigEndian(enormous.AsSpan(16, 4), 10000);
                BinaryPrimitives.WriteInt32BigEndian(enormous.AsSpan(20, 4), 10000);
                File.WriteAllBytes(Path.Combine(root, "for-mange-pixels.png"), enormous);
                using (var large = File.Create(Path.Combine(root, "for-stor.png"))) large.SetLength(20 * 1024 * 1024 + 1);
                File.WriteAllText(Path.Combine(root, "dokument.pdf"), "ikke et billede");
                var service = new LocalMediaInspectionService(root, root, null, null);
                var valid = await service.InspectAsync(new("1", "a", null, "Foto", "foto.png"), CancellationToken.None);
                valid.CanSelect.Should().BeTrue(valid.Status);
                valid.RelativePath.Should().Be("foto.png");
                (await service.InspectAsync(new("2", "a", null, null, "falsk.png"), CancellationToken.None)).CanSelect.Should().BeFalse();
                (await service.InspectAsync(new("3", "a", null, null, "dokument.pdf"), CancellationToken.None)).CanSelect.Should().BeFalse();
                (await service.InspectAsync(new("5", "a", null, null, "beskadiget.png"), CancellationToken.None)).CanSelect.Should().BeFalse();
                (await service.InspectAsync(new("6", "a", null, null, "for-mange-pixels.png"), CancellationToken.None)).Status.Should().Contain("40 millioner");
                (await service.InspectAsync(new("7", "a", null, null, "for-stor.png"), CancellationToken.None)).Status.Should().Contain("20 MB");
                (await service.InspectAsync(new("4", "a", null, null, "mangler.jpg"), CancellationToken.None)).CanSelect.Should().BeFalse();
            }
            finally { Directory.Delete(root, true); }
        });
    }
}
