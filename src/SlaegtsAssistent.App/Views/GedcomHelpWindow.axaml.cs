using System;
using Avalonia;
using Avalonia.Controls;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class GedcomHelpWindow : Window
{
    private bool _previewAdapterReady;

    public GedcomHelpWindow()
    {
        InitializeComponent();
        SafePreviewWebViewController.Attach(PreviewWebView, this);
        DataContextChanged += (_, _) => RefreshPreview();
        Closed += HandleClosed;
        Application.Current?.ActualThemeVariantChanged += HandleThemeVariantChanged;
        PreviewWebView.AdapterCreated += (_, _) =>
        {
            _previewAdapterReady = true;
            RefreshPreview();
        };
        PreviewWebView.AdapterDestroyed += (_, _) => _previewAdapterReady = false;
    }

    private void RefreshPreview()
    {
        if (_previewAdapterReady && DataContext is GedcomHelpViewModel viewModel)
        {
            PreviewWebView.NavigateToString(CheatSheetPreviewTheme.Apply(viewModel.PreviewHtmlDocument));
        }
    }

    private void HandleThemeVariantChanged(object? sender, EventArgs e)
    {
        RefreshPreview();
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        Application.Current?.ActualThemeVariantChanged -= HandleThemeVariantChanged;
    }
}
