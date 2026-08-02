using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using SlaegtsAssistent.Core.Biography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaGedcomDifferenceDialogService : IGedcomDifferenceDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaGedcomDifferenceDialogService(
        IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
        IReadOnlyList<GedcomDifferenceReviewItem> differences)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null || differences.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<string, bool>?>(null);
        }

        var gedcomChoices = new Dictionary<string, RadioButton>(StringComparer.Ordinal);
        var rows = new StackPanel { Spacing = 6 };

        foreach (var difference in differences)
        {
            var markdownChoice = new RadioButton
            {
                Content = "Markdown",
                GroupName = difference.Key,
                IsChecked = !difference.UseGedcomByDefault,
            };
            var gedcomChoice = new RadioButton
            {
                Content = "GEDCOM",
                GroupName = difference.Key,
                IsChecked = difference.UseGedcomByDefault,
            };
            gedcomChoices[difference.Key] = gedcomChoice;
            var choicePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { markdownChoice, gedcomChoice },
            };

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("170,130,*,*,210"),
                ColumnSpacing = 8,
            };
            row.Children.Add(CreateValueBlock(difference.PersonName, 0, FontWeight.SemiBold));
            row.Children.Add(CreateValueBlock(difference.Difference.FieldName, 1, FontWeight.SemiBold));
            row.Children.Add(CreateValueBlock(difference.Difference.DocumentValue, 2));
            row.Children.Add(CreateValueBlock(difference.Difference.GedcomValue, 3));
            Grid.SetColumn(choicePanel, 4);
            row.Children.Add(choicePanel);
            rows.Children.Add(row);
        }

        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, bool>?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyDictionary<string, bool>? selectedResult = null;
        var dialog = new Window
        {
            Title = "Forskelle mellem GEDCOM og Markdown",
            Width = 1120,
            Height = Math.Min(760, 230 + differences.Count * 48),
            MinWidth = 900,
            MinHeight = 320,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Anvend valgte", MinWidth = 130 };
        applyButton.Click += (_, _) =>
        {
            selectedResult = gedcomChoices.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.IsChecked == true,
                StringComparer.Ordinal);
            dialog.Close();
        };
        var closeButton = new Button { Content = "Luk uden ændringer", MinWidth = 150 };
        closeButton.Click += (_, _) => dialog.Close();

        var buttonBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { closeButton, applyButton },
        };
        DockPanel.SetDock(buttonBar, Dock.Bottom);

        dialog.Content = new DockPanel
        {
            Margin = new Thickness(18),
            Children =
            {
                buttonBar,
                new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "GEDCOM-filen indeholder værdier, der afviger fra den fortolkede faktasektion i Markdown. Vælg kilde for hvert felt.",
                                TextWrapping = TextWrapping.Wrap,
                            },
                            CreateHeaderRow(),
                            rows,
                        },
                    },
                },
            },
        };

        dialog.Closed += (_, _) => completion.TrySetResult(selectedResult);
        dialog.Show(owner);
        return completion.Task;
    }

    private static Grid CreateHeaderRow()
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("170,130,*,*,210"),
            ColumnSpacing = 8,
        };
        row.Children.Add(CreateValueBlock("Person", 0, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Felt", 1, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Markdown", 2, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("GEDCOM", 3, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Kilde", 4, FontWeight.Bold));
        return row;
    }

    private static TextBlock CreateValueBlock(
        string? value,
        int column,
        FontWeight? fontWeight = null)
    {
        var block = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (fontWeight is { } weight)
        {
            block.FontWeight = weight;
        }

        Grid.SetColumn(block, column);
        return block;
    }
}
