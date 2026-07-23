using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaGedcomFilePickerService : IGedcomFilePickerService
{
    private static readonly FilePickerFileType GedcomFileType = new("GEDCOM-filer")
    {
        Patterns = ["*.ged"],
        AppleUniformTypeIdentifiers = ["public.text"],
        MimeTypes = ["text/plain"],
    };

    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaGedcomFilePickerService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<string?> PickGedcomFileAsync()
    {
        var mainWindow = _applicationLifetime.MainWindow;
        if (mainWindow is null)
        {
            return null;
        }

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Vaelg GEDCOM-fil",
            AllowMultiple = false,
            FileTypeFilter = [GedcomFileType],
        });

        if (files.Count == 0)
        {
            return null;
        }

        return files[0].TryGetLocalPath();
    }
}
