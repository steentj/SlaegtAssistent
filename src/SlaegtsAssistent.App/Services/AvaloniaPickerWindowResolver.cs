using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;

namespace SlaegtsAssistent.App.Services;

internal static class AvaloniaPickerWindowResolver
{
    public static Window? Resolve(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        return applicationLifetime.Windows.FirstOrDefault(window => window.IsActive)
            ?? applicationLifetime.MainWindow;
    }
}