using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.IO;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaTemplateFilePickerService : ITemplateFilePickerService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaTemplateFilePickerService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<string?> PickTemplateFileAsync(string? suggestedStartFolder)
    {
        var mainWindow = _applicationLifetime.MainWindow;
        if (mainWindow is null)
        {
            return null;
        }

        IStorageFolder? suggestedStartLocation = null;
        if (!string.IsNullOrWhiteSpace(suggestedStartFolder) && Directory.Exists(suggestedStartFolder))
        {
            suggestedStartLocation = await mainWindow.StorageProvider.TryGetFolderFromPathAsync(suggestedStartFolder);
        }

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Vælg persondokumentskabelon",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeFilter =
            [
                new FilePickerFileType("Markdown- og tekstskabeloner")
                {
                    Patterns = ["*.md", "*.txt"],
                },
            ],
        });

        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }
}
