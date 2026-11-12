using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Views;

public sealed class PersonGraphControl : Control
{
    private GraphCamera _camera = new(new GraphPoint(0, 0), 1);
    private PersonGraphViewModel? _model;
    private Point? _pointerDown;
    private Point _previous;
    private string? _pressedId;
    private bool _dragged;
    private bool _fitPending;
    private bool _middleDrag;
    private GraphSpatialIndex? _spatialIndex;

    public (GraphPoint Origin, GraphBounds Bounds) GetVisibleWorldBounds()
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || _camera.Zoom <= 0)
            throw new InvalidOperationException("Grafens udsnit er ikke tilgængeligt.");
        return (_camera.ToWorld(new GraphPoint(0, 0)), new GraphBounds(Bounds.Width / _camera.Zoom, Bounds.Height / _camera.Zoom));
    }

    public PersonGraphControl()
    {
        DataContextChanged += (_, _) => AttachModel(DataContext as PersonGraphViewModel);
        ClipToBounds = true;
        Focusable = true;
    }

    private void AttachModel(PersonGraphViewModel? model)
    {
        if (_model is not null) _model.PropertyChanged -= ModelChanged;
        _model = model;
        _spatialIndex = model?.Layout is { } initial ? new GraphSpatialIndex(initial.Positions) : null;
        if (_model is not null) _model.PropertyChanged += ModelChanged;
        _fitPending = true;
        Fit();
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_model is not null) _model.PropertyChanged -= ModelChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_model is not null)
        {
            _model.PropertyChanged -= ModelChanged;
            _model.PropertyChanged += ModelChanged;
        }
    }

    private void ModelChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(PersonGraphViewModel.Layout))
        {
            _spatialIndex = _model?.Layout is { } layout ? new GraphSpatialIndex(layout.Positions) : null;
            _fitPending = true; Fit();
        }
        if (args.PropertyName == nameof(PersonGraphViewModel.SelectedPerson) && _model?.SelectedPerson is { } selected)
            CenterPerson(selected.RecordId);
        InvalidateVisual();
    }

    private void Fit()
    {
        if (_model?.Layout is { } layout && Bounds.Width > 0 && Bounds.Height > 0)
        {
            _camera = GraphCamera.Fit(layout.Bounds, Bounds.Width, Bounds.Height);
            _fitPending = false;
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_fitPending) Fit();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_model?.Layout is not { } layout) return;
        var edgePen = new Pen(Brushes.Gray, 0.8);
        foreach (var edge in _model.Graph.Edges)
        {
            if (!layout.Positions.TryGetValue(edge.FirstId, out var first) || !layout.Positions.TryGetValue(edge.SecondId, out var second)) continue;
            var a = _camera.ToScreen(first); var b = _camera.ToScreen(second);
            if ((a.X < 0 && b.X < 0) || (a.Y < 0 && b.Y < 0) ||
                (a.X > Bounds.Width && b.X > Bounds.Width) || (a.Y > Bounds.Height && b.Y > Bounds.Height)) continue;
            context.DrawLine(edgePen, new Point(a.X, a.Y), new Point(b.X, b.Y));
        }
        foreach (var node in _model.Graph.Nodes)
        {
            if (!layout.Positions.TryGetValue(node.RecordId, out var world)) continue;
            var p = _camera.ToScreen(world);
            var radius = Math.Clamp(4 * _camera.Zoom, 3, 10) * (node.RecordId == _model.ProbandId ? 1.8 : 1);
            if (p.X < -radius || p.Y < -radius || p.X > Bounds.Width + radius || p.Y > Bounds.Height + radius) continue;
            var brush = node.Sex?.Trim().ToUpperInvariant() switch
            {
                "F" => new SolidColorBrush(Color.Parse("#DC3C4D")),
                "M" => new SolidColorBrush(Color.Parse("#3478D4")),
                _ => new SolidColorBrush(Color.Parse("#8A8F98")),
            };
            var outline = ActualThemeVariant == ThemeVariant.Dark ? Brushes.White : Brushes.Black;
            context.DrawEllipse(brush, node.RecordId == _model.ProbandId ? new Pen(outline, 2) : null, new Point(p.X, p.Y), radius, radius);
        }
        if (_model.HoveredPerson is not null)
        {
            var placed = new List<Rect>();
            var dark = ActualThemeVariant == ThemeVariant.Dark;
            var foreground = dark ? Brushes.White : Brushes.Black;
            var background = dark ? Brushes.Black : Brushes.White;
            foreach (var item in _model.DirectConnections)
            {
                if (!layout.Positions.TryGetValue(item.RecordId, out var world)) continue;
                var p = _camera.ToScreen(world);
                var text = new FormattedText(item.Details, CultureInfo.GetCultureInfo("da-DK"), FlowDirection.LeftToRight,
                    new Typeface("Inter"), 12, foreground);
                var x = Math.Clamp(p.X + 9, 0, Math.Max(0, Bounds.Width - text.Width - 4));
                var y = Math.Clamp(p.Y + 6, 0, Math.Max(0, Bounds.Height - text.Height - 4));
                var label = new Rect(x, y, text.Width + 4, text.Height + 4);
                for (var attempt = 0; attempt < 50 && placed.Any(other => other.Intersects(label)); attempt++)
                {
                    y += text.Height + 6;
                    if (y + text.Height + 4 > Bounds.Height) { y = 0; x += text.Width + 8; }
                    label = new Rect(x, y, text.Width + 4, text.Height + 4);
                }
                placed.Add(label);
                context.DrawLine(new Pen(foreground, 0.5), new Point(p.X, p.Y), label.TopLeft);
                context.DrawRectangle(background, null, label);
                context.DrawText(text, new Point(label.X + 2, label.Y + 2));
            }
        }
    }

    private string? Hit(Point position)
    {
        var world = _camera.ToWorld(new GraphPoint(position.X, position.Y));
        return _spatialIndex?.FindNearest(world, 10 / _camera.Zoom);
    }

    public void ShowAll() { Fit(); InvalidateVisual(); }

    public void ZoomBy(double factor)
    {
        _camera = _camera.ZoomAt(new GraphPoint(Bounds.Width / 2, Bounds.Height / 2), _camera.Zoom * factor);
        InvalidateVisual();
    }

    public void CenterProband()
    {
        if (_model?.ProbandId is { } id) CenterPerson(id);
    }

    private void CenterPerson(string id)
    {
        if (_model?.Layout?.Positions.TryGetValue(id, out var point) != true) return;
        _camera = new GraphCamera(new GraphPoint(point.X - Bounds.Width / (2 * _camera.Zoom), point.Y - Bounds.Height / (2 * _camera.Zoom)), _camera.Zoom);
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var p = e.GetPosition(this);
        _camera = _camera.ZoomAt(new GraphPoint(p.X, p.Y), _camera.Zoom * Math.Pow(1.15, e.Delta.Y));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsLeftButtonPressed && !properties.IsMiddleButtonPressed) return;
        _middleDrag = properties.IsMiddleButtonPressed;
        _pointerDown = _previous = e.GetPosition(this);
        _pressedId = Hit(_previous);
        _dragged = false;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        if (_pointerDown is { } down)
        {
            if (Math.Abs(point.X - down.X) > 4 || Math.Abs(point.Y - down.Y) > 4) _dragged = true;
            if (_dragged && (_pressedId is null || _middleDrag)) _camera = _camera.Pan(point.X - _previous.X, point.Y - _previous.Y);
            _previous = point;
        }
        else _model?.Hover(Hit(point));
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointerDown is not null && !_dragged && !_middleDrag && _pressedId is not null && Hit(e.GetPosition(this)) == _pressedId)
        {
            _model?.Select(_pressedId);
            _model?.OpenBiographyCommand.Execute(null);
        }
        _pointerDown = null;
        _pressedId = null;
        _middleDrag = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_pointerDown is null) _model?.Hover(null);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pointerDown = null;
        _pressedId = null;
        _middleDrag = false;
    }
}
