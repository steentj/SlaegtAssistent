using Avalonia.Controls;
using Avalonia;
using System;
using System.ComponentModel;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Views;

public partial class MarkdownCheatSheetWindow : Window
{
    private MarkdownCheatSheetViewModel? _viewModel;
    private bool _previewAdapterReady;

    public MarkdownCheatSheetWindow()
    {
        InitializeComponent();
        SafePreviewWebViewController.Attach(PreviewWebView, this);
        DataContextChanged += HandleDataContextChanged;
        Opened += (_, _) => FocusSearch();
        Closed += HandleClosed;
        Application.Current?.ActualThemeVariantChanged += HandleThemeVariantChanged;
        PreviewWebView.AdapterCreated += (_, _) =>
        {
            _previewAdapterReady = true;
            RefreshPreview();
        };
        PreviewWebView.AdapterDestroyed += (_, _) => _previewAdapterReady = false;
    }

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        }

        _viewModel = DataContext as MarkdownCheatSheetViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        RefreshPreview();
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarkdownCheatSheetViewModel.PreviewHtml)
            or nameof(MarkdownCheatSheetViewModel.PreviewHtmlDocument))
        {
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        if (_previewAdapterReady && _viewModel is not null)
        {
            PreviewWebView.NavigateToString(CheatSheetPreviewTheme.Apply(_viewModel.PreviewHtmlDocument));
        }
    }

    public void FocusSearch()
    {
        Activate();
        SearchBox.Focus();
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
