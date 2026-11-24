using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class DocumentImageWindow : Window
{
    private readonly DocumentImageZoom _zoom = new();
    private CancellationTokenSource? _loading;
    private Bitmap? _bitmap;

    public DocumentImageWindow()
    {
        InitializeComponent();
        ImageArea.SizeChanged += (_, _) => UpdateImageSize();
        ImageScroll.AddHandler(PointerWheelChangedEvent, ZoomWithWheel, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, CloseWithEscape, RoutingStrategies.Tunnel);
        Closed += (_, _) => ClearImage();
        UpdateImageSize();
    }

    internal async Task OpenImageAsync(DocumentMediaCard card,
        Func<CancellationToken, Task<LocalMediaInspectionService>> createInspection)
    {
        ClearImage();
        using var loading = new CancellationTokenSource();
        _loading = loading;
        Status.Text = "Indlæser billede…";
        Status.IsVisible = true;
        Title = card.Name;
        AutomationProperties.SetName(Photo, card.Name);
        Bitmap? bitmap = null;
        try
        {
            var inspection = await createInspection(loading.Token);
            var result = await inspection.ReadImageAsync(card.Item, null, (_, image) => bitmap = image, loading.Token);
            if (!ReferenceEquals(_loading, loading)) return;
            if (!result.CanSelect || bitmap is null)
            {
                Status.Text = result.Status;
                return;
            }

            _bitmap = bitmap;
            bitmap = null;
            Photo.Source = _bitmap;
            Status.IsVisible = false;
            _zoom.Fit();
            UpdateImageSize();
            ImageScroll.Focus();
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_loading, loading)) Status.Text = "Billedindlæsningen blev afbrudt.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (ReferenceEquals(_loading, loading)) Status.Text = "Billedet kunne ikke åbnes.";
        }
        finally
        {
            bitmap?.Dispose();
            if (ReferenceEquals(_loading, loading)) _loading = null;
        }
    }

    private void ClearImage()
    {
        var loading = _loading;
        _loading = null;
        loading?.Cancel();
        Photo.Source = null;
        _bitmap?.Dispose();
        _bitmap = null;
        ImageScroll.Offset = default;
        UpdateImageSize();
    }

    private void UpdateImageSize()
    {
        if (_bitmap is { } bitmap)
        {
            var size = bitmap.PixelSize.ToSizeWithDpi(new Vector(96, 96));
            _zoom.UpdateFit(size.Width, size.Height, ImageArea.Bounds.Width, ImageArea.Bounds.Height);
            Photo.Width = size.Width * _zoom.Scale;
            Photo.Height = size.Height * _zoom.Scale;
        }
        else
        {
            Photo.Width = 0;
            Photo.Height = 0;
        }

        IncreaseButton.IsEnabled = _bitmap is not null && _zoom.CanZoomIn;
        DecreaseButton.IsEnabled = _bitmap is not null && _zoom.CanZoomOut;
        ActualButton.IsEnabled = _bitmap is not null;
        FitButton.IsEnabled = _bitmap is not null;
        ZoomPercentage.Text = _bitmap is null ? "" : $"{_zoom.Scale * 100:0} %";
    }

    private void IncreaseZoom(object? sender, RoutedEventArgs args) => StepZoom(1);
    private void DecreaseZoom(object? sender, RoutedEventArgs args) => StepZoom(-1);

    private void ActualZoom(object? sender, RoutedEventArgs args)
    {
        _zoom.ActualSize();
        UpdateImageSize();
    }

    private void FitZoom(object? sender, RoutedEventArgs args)
    {
        _zoom.Fit();
        UpdateImageSize();
        ImageScroll.Offset = default;
    }

    private void StepZoom(int direction)
    {
        if (_bitmap is null) return;
        _zoom.StepBy(direction);
        UpdateImageSize();
    }

    private void ZoomWithWheel(object? sender, PointerWheelEventArgs args)
    {
        var modifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        if ((args.KeyModifiers & modifier) == 0 || args.Delta.Y == 0 || _bitmap is null) return;
        StepZoom(Math.Sign(args.Delta.Y));
        args.Handled = true;
    }

    private void CloseWithEscape(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Escape) return;
        args.Handled = true;
        Close();
    }
}
