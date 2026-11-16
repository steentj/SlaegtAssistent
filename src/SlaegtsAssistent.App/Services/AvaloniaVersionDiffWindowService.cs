using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Versioning;
using System.Collections.Generic;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaVersionDiffWindowService : IVersionDiffWindowService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;
    private Window? _window;

    public AvaloniaVersionDiffWindowService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public void Show(string title, IReadOnlyList<DiffFile> files, string? notice)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return;
        }

        if (_window is VersionDiffWindow existing && existing.DataContext is VersionDiffViewModel existingViewModel)
        {
            existingViewModel.SetContent(title, files, notice);
            existing.Activate();
            return;
        }

        var viewModel = new VersionDiffViewModel();
        viewModel.SetContent(title, files, notice);
        var window = new VersionDiffWindow
        {
            DataContext = viewModel,
        };
        _window = window;
        window.Closed += (_, _) => _window = null;
        window.Show(owner);
    }
}
