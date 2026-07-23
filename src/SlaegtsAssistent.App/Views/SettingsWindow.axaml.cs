using Avalonia.Controls;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(SettingsWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += HandleCloseRequested;
    }

    private void HandleCloseRequested(object? sender, AppSettings? result)
    {
        if (sender is SettingsWindowViewModel viewModel)
        {
            viewModel.CloseRequested -= HandleCloseRequested;
        }

        Close(result);
    }
}
