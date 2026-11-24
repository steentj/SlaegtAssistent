using System;

namespace SlaegtsAssistent.App.ViewModels;

internal sealed class DocumentImageZoom
{
    private const double Step = 0.25;
    private const double Maximum = 4;
    private double? _manualScale;

    public bool IsFit => _manualScale is null;
    public bool CanZoomIn => Scale < Maximum;
    public bool CanZoomOut => Scale > Step;
    public double Scale => _manualScale ?? _fitScale;
    private double _fitScale = 1;

    public void UpdateFit(double width, double height, double viewportWidth, double viewportHeight)
    {
        if (width > 0 && height > 0 && viewportWidth > 0 && viewportHeight > 0)
            _fitScale = Math.Min(1, Math.Min(viewportWidth / width, viewportHeight / height));
    }

    public void Fit() => _manualScale = null;

    public void ActualSize() => _manualScale = 1;

    public void StepBy(int direction)
    {
        if (direction > 0 && CanZoomIn)
            _manualScale = Math.Clamp((Math.Floor(Scale / Step) + 1) * Step, Step, Maximum);
        else if (direction < 0 && CanZoomOut)
            _manualScale = Math.Clamp((Math.Ceiling(Scale / Step) - 1) * Step, Step, Maximum);
    }
}
