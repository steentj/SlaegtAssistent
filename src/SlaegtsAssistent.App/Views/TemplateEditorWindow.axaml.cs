using Avalonia;
using Avalonia.Controls;
using System;
using System.ComponentModel;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Views;

public partial class TemplateEditorWindow : Window
{
    private TemplateEditorViewModel? _viewModel;
    private bool _previewAdapterReady;

    public TemplateEditorWindow()
    {
        InitializeComponent();
        SafePreviewWebViewController.Attach(TemplatePreviewWebView, this);
        DataContextChanged += HandleDataContextChanged;
        Closed += HandleClosed;
        Application.Current?.ActualThemeVariantChanged += HandleThemeVariantChanged;
        TemplatePreviewWebView.AdapterCreated += (_, _) =>
        {
            _previewAdapterReady = true;
            RefreshPreview();
        };
        TemplatePreviewWebView.AdapterDestroyed += (_, _) => _previewAdapterReady = false;
    }

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        }

        _viewModel = DataContext as TemplateEditorViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        RefreshPreview();
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TemplateEditorViewModel.PreviewHtmlDocument))
        {
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        if (_previewAdapterReady && _viewModel is not null &&
            !string.IsNullOrWhiteSpace(_viewModel.PreviewHtmlDocument))
        {
            TemplatePreviewWebView.NavigateToString(
                CheatSheetPreviewTheme.Apply(_viewModel.PreviewHtmlDocument));
        }
    }

    private void HandleThemeVariantChanged(object? sender, EventArgs e) => RefreshPreview();

    private void HandleClosed(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        }

        Application.Current?.ActualThemeVariantChanged -= HandleThemeVariantChanged;
    }

    public TemplateEditorWindow(TemplateEditorViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += HandleCloseRequested;
    }

    private void HandleCloseRequested(object? sender, BiographyTemplateEditorModel? result)
    {
        if (sender is TemplateEditorViewModel viewModel)
        {
            viewModel.CloseRequested -= HandleCloseRequested;
        }

        Close(result);
    }
}
