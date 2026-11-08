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
        var pickerWindow = AvaloniaPickerWindowResolver.Resolve(_applicationLifetime);
        if (pickerWindow is null)
        {
            return null;
        }

        IStorageFolder? suggestedStartLocation = null;
        if (!string.IsNullOrWhiteSpace(suggestedStartFolder) && Directory.Exists(suggestedStartFolder))
        {
            suggestedStartLocation = await pickerWindow.StorageProvider.TryGetFolderFromPathAsync(suggestedStartFolder);
        }

        var files = await pickerWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Vælg persondokumentskabelon",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeFilter =
            [
                new FilePickerFileType("Persondokumentskabeloner")
                {
                    Patterns = ["*.slaegtskabelon", "*.json", "*.md", "*.txt"],
                },
            ],
        });

        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }

    public async Task<string?> PickTemplateSaveFileAsync(string? suggestedPath)
    {
        var window = AvaloniaPickerWindowResolver.Resolve(_applicationLifetime);
        if (window is null)
        {
            return null;
        }

        var directory = Path.GetDirectoryName(suggestedPath);
        var folder = !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)
            ? await window.StorageProvider.TryGetFolderFromPathAsync(directory)
            : null;
        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Gem persondokumentskabelon som",
            SuggestedFileName = Path.GetFileName(suggestedPath) is { Length: > 0 } name ? name : "personskabelon.slaegtskabelon",
            SuggestedStartLocation = folder,
            DefaultExtension = "slaegtskabelon",
            ShowOverwritePrompt = true,
            FileTypeChoices = [new FilePickerFileType("Skabelonmodel") { Patterns = ["*.slaegtskabelon"] }],
        });
        return file?.Path.LocalPath;
    }
}
