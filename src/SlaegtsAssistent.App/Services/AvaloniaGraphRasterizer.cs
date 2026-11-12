using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SlaegtsAssistent.Core.Graph.Export;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaGraphRasterizer
{
    public byte[] Render(GraphExportScene scene, int longSide)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (longSide is not (2048 or 4096 or 8192) || !double.IsFinite(scene.Bounds.Width) || !double.IsFinite(scene.Bounds.Height) || scene.Bounds.Width <= 0 || scene.Bounds.Height <= 0)
            throw new ArgumentException("Grafens størrelse eller opløsning er ugyldig.");
        var scale = longSide / Math.Max(scene.Bounds.Width, scene.Bounds.Height);
        var width = Math.Max(1, (int)Math.Ceiling(scene.Bounds.Width * scale));
        var height = Math.Max(1, (int)Math.Ceiling(scene.Bounds.Height * scale));
        if ((long)width * height > 64_000_000) throw new ArgumentException("Billedet overstiger grænsen på 64 megapixel.");
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            using (context.PushTransform(Matrix.CreateScale(scale, scale)))
            {
                var pen = new Pen(Brushes.Gray, 1.5);
                foreach (var line in scene.Lines)
                    context.DrawLine(pen, new Point(line.Start.X, line.Start.Y), new Point(line.End.X, line.End.Y));
                foreach (var node in scene.Nodes)
                {
                    context.DrawEllipse(new SolidColorBrush(Color.Parse(node.Fill)), node.IsProband ? new Pen(Brushes.Black, 2) : null,
                        new Point(node.Position.X, node.Position.Y), node.Radius, node.Radius);
                    if (node.Label is null) continue;
                    var position = node.LabelPosition ?? new SlaegtsAssistent.Core.Graph.GraphPoint(node.Position.X, node.Position.Y + node.Radius + 4);
                    var lines = node.Label.Split('\n');
                    for (var index = 0; index < lines.Length; index++)
                    {
                        var text = new FormattedText(lines[index], CultureInfo.GetCultureInfo("da-DK"), FlowDirection.LeftToRight,
                            new Typeface("sans-serif"), 12, Brushes.Black);
                        context.DrawText(text, new Point(position.X, position.Y + index * 16));
                    }
                }
            }
        }
        using var output = new MemoryStream();
        bitmap.Save(output, PngBitmapEncoderOptions.Default);
        return output.ToArray();
    }
}
