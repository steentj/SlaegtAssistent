namespace SlaegtsAssistent.Core.Graph;

public readonly record struct GraphCamera(GraphPoint Origin, double Zoom)
{
    public GraphPoint ToScreen(GraphPoint point) => new((point.X - Origin.X) * Zoom, (point.Y - Origin.Y) * Zoom);

    public GraphPoint ToWorld(GraphPoint point)
    {
        if (!double.IsFinite(Zoom) || Zoom <= 0) throw new InvalidOperationException("Grafens zoom er ugyldig.");
        return new GraphPoint(point.X / Zoom + Origin.X, point.Y / Zoom + Origin.Y);
    }

    public GraphCamera ZoomAt(GraphPoint screenPoint, double requestedZoom)
    {
        if (!double.IsFinite(requestedZoom) || requestedZoom <= 0) throw new ArgumentOutOfRangeException(nameof(requestedZoom));
        var world = ToWorld(screenPoint);
        var zoom = Math.Clamp(requestedZoom, 0.1, 8);
        return new GraphCamera(new GraphPoint(world.X - screenPoint.X / zoom, world.Y - screenPoint.Y / zoom), zoom);
    }

    public GraphCamera Pan(double screenDeltaX, double screenDeltaY)
    {
        if (!double.IsFinite(Zoom) || Zoom <= 0) throw new InvalidOperationException("Grafens zoom er ugyldig.");
        return new GraphCamera(new GraphPoint(Origin.X - screenDeltaX / Zoom, Origin.Y - screenDeltaY / Zoom), Zoom);
    }

    public static GraphCamera Fit(GraphBounds bounds, double width, double height, double margin = 24)
    {
        if (!double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) || !double.IsFinite(width) || !double.IsFinite(height) ||
            !double.IsFinite(margin) || bounds.Width <= 0 || bounds.Height <= 0 || width <= 0 || height <= 0 || margin < 0)
            throw new ArgumentOutOfRangeException(nameof(bounds), "Grafens størrelse er ugyldig.");
        var zoom = Math.Clamp(Math.Min(Math.Max(1, width - 2 * margin) / bounds.Width, Math.Max(1, height - 2 * margin) / bounds.Height), 0.1, 8);
        return new GraphCamera(new GraphPoint((bounds.Width - width / zoom) / 2, (bounds.Height - height / zoom) / 2), zoom);
    }
}
