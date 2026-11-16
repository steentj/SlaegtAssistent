using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class VersionHistoryWindow : Window
{
    public VersionHistoryWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Opened += (_, _) =>
        {
            if (DataContext is VersionHistoryViewModel vm)
            {
                _ = vm.RefreshAsync();
            }
        };
        Activated += (_, _) =>
        {
            if (DataContext is VersionHistoryViewModel vm)
            {
                _ = vm.RefreshAsync();
            }
        };
        Closed += (_, _) =>
        {
            if (DataContext is VersionHistoryViewModel vm)
            {
                vm.Dispose();
            }
        };
    }
}
