using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaGedcomFilePickerService : IGedcomFilePickerService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaGedcomFilePickerService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<string?> PickGedcomFileAsync(string? suggestedStartFolder)
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

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(
            CreateOpenOptions(suggestedStartLocation));

        if (files.Count == 0)
        {
            return null;
        }

        return ResolveSelectedFilePath(files[0].Path);
    }

    public static FilePickerOpenOptions CreateOpenOptions(IStorageFolder? suggestedStartLocation)
    {
        return new FilePickerOpenOptions
        {
            Title = "Vælg GEDCOM-fil",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation,
        };
    }

    public static string? ResolveSelectedFilePath(Uri path)
    {
        return path.LocalPath;
    }
}
