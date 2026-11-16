using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SkiaSharp;

namespace SlaegtsAssistent.App.Tests;

public class ExportResourceCollectorTests
{
    [Fact]
    public void HoejtBilledeTilpassesSidehoeJdenProportionalt()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (var bitmap = new SKBitmap(400, 1600))
            {
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(Path.Combine(root, "foto.png"), data.ToArray());
            }
            var assets = Path.Combine(root, "assets");
            var relative = new ExportResourceCollector(assets, []).Collect("foto.png", Path.Combine(root, "person.md"));
            relative.Should().NotBeNull();
            using var result = SKBitmap.Decode(Path.Combine(assets, Path.GetFileName(relative!)!));
            result.Width.Should().Be(242);
            result.Height.Should().Be(968);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void EksportBevarerExifDrejningVedNedskalering()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            byte[] jpeg;
            using (var bitmap = new SKBitmap(1600, 800))
            using (var canvas = new SKCanvas(bitmap))
            using (var paint = new SKPaint { Color = SKColors.Red })
            {
                canvas.Clear(SKColors.Blue);
                canvas.DrawRect(0, 0, 800, 800, paint);
                using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);
                jpeg = data.ToArray();
            }
            byte[] exif = [0xff, 0xe1, 0x00, 0x22, 0x45, 0x78, 0x69, 0x66, 0, 0,
                0x49, 0x49, 0x2a, 0, 8, 0, 0, 0, 1, 0, 0x12, 1, 3, 0, 1, 0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0];
            File.WriteAllBytes(Path.Combine(root, "foto.jpg"), jpeg[..2].Concat(exif).Concat(jpeg[2..]).ToArray());
            var assets = Path.Combine(root, "assets");
            var relative = new ExportResourceCollector(assets, []).Collect("foto.jpg", Path.Combine(root, "person.md"), 25);
            relative.Should().NotBeNull();
            using var result = SKBitmap.Decode(Path.Combine(assets, Path.GetFileName(relative)));
            result.Width.Should().Be(160);
            result.Height.Should().Be(320);
            result.GetPixel(80, 40).Red.Should().BeGreaterThan(result.GetPixel(80, 40).Blue);
            result.GetPixel(80, 280).Blue.Should().BeGreaterThan(result.GetPixel(80, 280).Red);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void MindreBilledeForstoerresIkkeOgFaarEnsBilledtaethedTilEksport()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (var bitmap = new SKBitmap(250, 125))
            {
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(Path.Combine(root, "foto.png"), data.ToArray());
            }
            var assets = Path.Combine(root, "assets");
            var relative = new ExportResourceCollector(assets, []).Collect("foto.png", Path.Combine(root, "person.md"), 50);
            relative.Should().NotBeNull();
            var result = File.ReadAllBytes(Path.Combine(assets, Path.GetFileName(relative!)!));
            using var bitmapResult = SKBitmap.Decode(result);
            bitmapResult.Width.Should().Be(250);
            bitmapResult.Height.Should().Be(125);
            result.Should().Contain("pHYs"u8.ToArray());
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(100, 642)]
    [InlineData(50, 321)]
    [InlineData(25, 160)]
    public void EksportTilpasserBillederProportionaltUdenAtAEndreOriginal(int widthPercent, int expectedWidth)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "foto.png");
            using (var bitmap = new SKBitmap(1600, 800))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.CornflowerBlue);
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(source, data.ToArray());
            }
            var original = File.ReadAllBytes(source);
            var assets = Path.Combine(root, "assets");
            var collector = new ExportResourceCollector(assets, []);
            var relative = collector.Collect("foto.png", Path.Combine(root, "person.md"), widthPercent);
            relative.Should().NotBeNull();
            var exported = File.ReadAllBytes(Path.Combine(assets, Path.GetFileName(relative)));
            using var image = SKBitmap.Decode(exported);
            image.Width.Should().Be(expectedWidth);
            image.Height.Should().Be(expectedWidth / 2);
            File.ReadAllBytes(source).Should().Equal(original);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void KopiererSigneretPngOgAfviserStiudbrudOgEksterneRessourcer()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var docs = Path.Combine(root, "docs");
        var assets = Path.Combine(root, "job", "assets");
        Directory.CreateDirectory(docs);
        try
        {
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC");
            File.WriteAllBytes(Path.Combine(docs, "foto.png"), png);
            File.WriteAllBytes(Path.Combine(root, "udenfor.png"), png);
            var collector = new ExportResourceCollector(assets, []);
            var path = collector.Collect("foto.png", Path.Combine(docs, "person.md"));
            path.Should().StartWith("assets/").And.EndWith(".png");
            collector.Collect("foto.png", Path.Combine(docs, "person.md")).Should().Be(path);
            using (var copied = SKBitmap.Decode(Path.Combine(assets, Path.GetFileName(path))))
            {
                copied.Width.Should().Be(180);
                copied.Height.Should().Be(120);
            }
            File.ReadAllBytes(Path.Combine(docs, "foto.png")).Should().Equal(png);
            collector.Collect("../udenfor.png", Path.Combine(docs, "person.md")).Should().BeNull();
            collector.Collect("https://example.com/a.png", Path.Combine(docs, "person.md")).Should().BeNull();
            File.WriteAllBytes(Path.Combine(docs, "forkert.svg"), png);
            collector.Collect("forkert.svg", Path.Combine(docs, "person.md")).Should().BeNull();
            File.WriteAllText(Path.Combine(docs, "falsk.png"), "ikke et billede");
            collector.Collect("falsk.png", Path.Combine(docs, "person.md")).Should().BeNull();
        }
        finally { Directory.Delete(root, true); }
    }
    [Fact]
    public void SymbolskLinkUdAfTilladtMappeAfvises()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var docs = Path.Combine(root, "docs");
        Directory.CreateDirectory(docs);
        try
        {
            File.WriteAllBytes(Path.Combine(root, "udenfor.png"), Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC"));
            File.CreateSymbolicLink(Path.Combine(docs, "link.png"), Path.Combine(root, "udenfor.png"));
            var collector = new ExportResourceCollector(Path.Combine(root, "job", "assets"), []);
            collector.Collect("link.png", Path.Combine(docs, "person.md")).Should().BeNull();
        }
        finally { Directory.Delete(root, true); }
    }
}
