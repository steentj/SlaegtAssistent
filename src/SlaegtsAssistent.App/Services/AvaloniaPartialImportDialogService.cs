using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaPartialImportDialogService : IPartialImportDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaPartialImportDialogService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<bool> ConfirmAsync(GedcomImportReport report)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return false;
        }

        var result = false;
        var acceptButton = new Button { Content = "Fortsæt med delvis import", MinWidth = 170 };
        var rejectButton = new Button { Content = "Afvis import", MinWidth = 110 };
        var details = string.Join(
            Environment.NewLine,
            report.Diagnostics.Select(diagnostic =>
                $"Linje {diagnostic.Line?.ToString() ?? "–"}, {diagnostic.Tag ?? "ukendt tag"}: " +
                $"{diagnostic.Message} Konsekvens: {diagnostic.Consequence}"));
        var dialog = new Window
        {
            Title = "Gennemgå delvis GEDCOM-import",
            Width = 680,
            Height = 480,
            MinWidth = 560,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                RowSpacing = 12,
                Margin = new Thickness(18),
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Importerede poster: {report.ImportedRecords}. " +
                               $"Med advarsler: {report.ImportedWithWarnings}. " +
                               $"Oversprungne: {report.SkippedRecords}.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBox
                    {
                        [Grid.RowProperty] = 1,
                        Text = details,
                        IsReadOnly = true,
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        [Grid.RowProperty] = 2,
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { rejectButton, acceptButton },
                    },
                },
            },
        };

        acceptButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        rejectButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
        return result;
    }
}
