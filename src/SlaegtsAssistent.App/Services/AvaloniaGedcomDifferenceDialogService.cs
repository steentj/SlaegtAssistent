using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaGedcomDifferenceDialogService : IGedcomDifferenceDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaGedcomDifferenceDialogService(
        IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
        string personName,
        IReadOnlyList<BiographyDifference> differences)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null)
        {
            return null;
        }

        var choices = new Dictionary<string, ComboBox>(StringComparer.Ordinal);
        var rows = new StackPanel { Spacing = 8 };

        foreach (var difference in differences)
        {
            var choice = new ComboBox
            {
                ItemsSource = new[] { "Behold tekst", "Brug GEDCOM" },
                SelectedIndex = 0,
                MinWidth = 130,
            };
            choices[difference.FieldName] = choice;

            rows.Children.Add(new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("150,*,*,Auto"),
                ColumnSpacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = difference.FieldName,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    CreateValueBlock(difference.DocumentValue, 1),
                    CreateValueBlock(difference.GedcomValue, 2),
                    choice,
                },
            });
        }

        var dialog = new Window
        {
            Title = $"GEDCOM-forskelle: {personName}",
            Width = 820,
            Height = Math.Min(620, 180 + differences.Count * 58),
            MinWidth = 700,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DockPanel
            {
                Margin = new Thickness(18),
                Children =
                {
                    CreateButtons(out var applyButton, out var cancelButton),
                    new ScrollViewer
                    {
                        Content = new StackPanel
                        {
                            Spacing = 14,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "GEDCOM indeholder andre værdier. Vælg for hvert felt, om dokumentets metadata skal beholdes eller opdateres.",
                                    TextWrapping = TextWrapping.Wrap,
                                },
                                new Grid
                                {
                                    ColumnDefinitions = new ColumnDefinitions("150,*,*,Auto"),
                                    ColumnSpacing = 8,
                                    Children =
                                    {
                                        CreateHeaderBlock("Felt", 0),
                                        CreateHeaderBlock("Dokument", 1),
                                        CreateHeaderBlock("GEDCOM", 2),
                                        CreateHeaderBlock("Valg", 3),
                                    },
                                },
                                rows,
                            },
                        },
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    },
                },
            },
        };

        applyButton.Click += (_, _) => dialog.Close(
            choices.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.SelectedIndex == 1,
                StringComparer.Ordinal));
        cancelButton.Click += (_, _) => dialog.Close();

        return await dialog.ShowDialog<IReadOnlyDictionary<string, bool>?>(owner);
    }

    private static TextBlock CreateValueBlock(string? value, int column)
    {
        var block = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(block, column);
        return block;
    }

    private static StackPanel CreateButtons(
        out Button applyButton,
        out Button cancelButton)
    {
        applyButton = new Button
        {
            Content = "Anvend valgte",
            MinWidth = 130,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        cancelButton = new Button
        {
            Content = "Annuller",
            MinWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancelButton, applyButton },
        };
    }

    private static TextBlock CreateHeaderBlock(string text, int column)
    {
        var block = new TextBlock
        {
            Text = text,
            FontWeight = Avalonia.Media.FontWeight.Bold,
        };
        Grid.SetColumn(block, column);
        return block;
    }
}
