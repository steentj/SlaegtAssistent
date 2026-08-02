using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = ConfigureServices(desktop);
            _services = services;
            var settings = services.GetRequiredService<IApplicationSettingsService>().Load();
            RequestedThemeVariant = settings.Theme switch
            {
                ThemePreference.Light => ThemeVariant.Light,
                ThemePreference.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
            desktop.MainWindow = services.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) => services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var services = new ServiceCollection();

        services.AddSingleton(desktop);
        services.AddSingleton<IGedcomLoader, GedcomLoader>();
        services.AddSingleton<IGedcomFilePickerService, AvaloniaGedcomFilePickerService>();
        services.AddSingleton<IFolderPickerService, AvaloniaFolderPickerService>();
        services.AddSingleton<ITemplateFilePickerService, AvaloniaTemplateFilePickerService>();
        services.AddSingleton<IMarkdownCheatSheetService, AvaloniaMarkdownCheatSheetService>();
        services.AddSingleton<IApplicationSettingsService, JsonApplicationSettingsService>();
        services.AddSingleton<ISettingsDialogService, AvaloniaSettingsDialogService>();
        services.AddSingleton<IUserDialogService, AvaloniaUserDialogService>();
        services.AddSingleton<IUnsavedChangesDialogService, AvaloniaUnsavedChangesDialogService>();
        services.AddSingleton<IApplicationControlService, AvaloniaApplicationControlService>();
        services.AddSingleton<IMarkdownBiographyExportService, MarkdownBiographyExportService>();
        services.AddSingleton<IMarkdownFileStore, FileSystemMarkdownFileStore>();
        services.AddSingleton<IMarkdownDocumentCatalog, FileSystemMarkdownDocumentCatalog>();
        services.AddSingleton<IGedcomDifferenceDialogService, AvaloniaGedcomDifferenceDialogService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>(provider => new MainWindow(
            provider.GetRequiredService<MainWindowViewModel>()));

        return services.BuildServiceProvider();
    }
}