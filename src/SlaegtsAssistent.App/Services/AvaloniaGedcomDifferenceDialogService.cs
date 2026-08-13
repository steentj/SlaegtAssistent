using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.App.ViewModels;
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
        var approvedChoices = new Dictionary<string, RadioButton>(StringComparer.Ordinal);
        var reviewState = new GedcomDifferenceReviewViewModel(differences);
        var rows = new StackPanel { Spacing = 6 };
        var preview = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            MinHeight = 180,
            FontFamily = new FontFamily("monospace"),
        };

        void UpdatePreview()
        {
            var content = reviewState.PreviewContent;
            preview.Text = content is null
                ? "Ingen kandidat anvendes med de aktuelle valg."
                : BiographyDocumentParser.Parse(content).Body;
        }

        foreach (var difference in differences)
        {
            var structured = difference.StructuredDifference;
            var markdownChoice = new RadioButton
            {
                Content = structured?.Causes.HasFlag(BiographyDifferenceCause.BaselineMigration) == true
                    ? "_Bevar"
                    : "_Dokument",
                GroupName = difference.Key,
                IsChecked = !difference.UseGedcomByDefault,
            };
            var gedcomChoice = new RadioButton
            {
                Content = structured?.Causes.HasFlag(BiographyDifferenceCause.BaselineMigration) == true
                    ? "_Migrér"
                    : "Ny _GEDCOM",
                GroupName = difference.Key,
                IsChecked = difference.UseGedcomByDefault,
            };
            markdownChoice.IsCheckedChanged += (_, _) =>
            {
                if (markdownChoice.IsChecked == true)
                {
                    reviewState.SetChoice(difference.Key, false);
                    UpdatePreview();
                }
            };
            gedcomChoice.IsCheckedChanged += (_, _) =>
            {
                if (gedcomChoice.IsChecked == true)
                {
                    reviewState.SetChoice(difference.Key, true);
                    UpdatePreview();
                }
            };
            gedcomChoices[difference.Key] = gedcomChoice;
            approvedChoices[difference.Key] = markdownChoice;
            var choicePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { markdownChoice, gedcomChoice },
            };

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("145,190,95,135,*,*,*,210"),
                ColumnSpacing = 8,
            };
            row.Children.Add(CreateValueBlock(difference.PersonName, 0, FontWeight.SemiBold));
            var fieldName = difference.BaselineStatus switch
            {
                BiographyBaselineStatus.Missing => $"{difference.Difference.FieldName}\nBaseline mangler",
                BiographyBaselineStatus.UnsupportedVersion => $"{difference.Difference.FieldName}\nUkendt baselineversion",
                _ => difference.Difference.FieldName,
            };
            row.Children.Add(CreateValueBlock(structured?.Path ?? fieldName, 1, FontWeight.SemiBold));
            row.Children.Add(CreateValueBlock(ActionText(structured), 2));
            row.Children.Add(CreateValueBlock(CauseText(structured), 3));
            row.Children.Add(CreateValueBlock(structured?.DocumentValue ?? difference.Difference.DocumentValue, 4));
            row.Children.Add(CreateValueBlock(structured?.ApprovedValue ?? difference.Difference.DocumentValue, 5));
            row.Children.Add(CreateValueBlock(structured?.ImportedValue ?? difference.Difference.GedcomValue, 6));
            Grid.SetColumn(choicePanel, 7);
            row.Children.Add(choicePanel);
            rows.Children.Add(row);
        }

        UpdatePreview();

        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, bool>?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyDictionary<string, bool>? selectedResult = null;
        var dialog = new Window
        {
            Title = "Forskelle mellem GEDCOM og Markdown",
            Width = 1480,
            Height = Math.Min(760, 230 + differences.Count * 48),
            MinWidth = 900,
            MinHeight = 320,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "_Anvend valgte", MinWidth = 130, IsDefault = true };
        applyButton.Click += (_, _) =>
        {
            selectedResult = reviewState.CreateDecision();
            dialog.Close();
        };
        var closeButton = new Button { Content = "_Luk uden ændringer", MinWidth = 150, IsCancel = true };
        closeButton.Click += (_, _) => dialog.Close();
        var keepAllButton = new Button { Content = "Bevar alle dokumentværdier", MinWidth = 195 };
        keepAllButton.Click += (_, _) =>
        {
            reviewState.KeepAllDocumentValues();
            foreach (var choice in approvedChoices.Values)
            {
                choice.IsChecked = true;
            }

            UpdatePreview();
        };
        var useAllButton = new Button { Content = "Vælg alle nye GEDCOM-værdier", MinWidth = 215 };
        useAllButton.Click += (_, _) =>
        {
            reviewState.UseAllImported();
            foreach (var choice in gedcomChoices.Values)
            {
                choice.IsChecked = true;
            }

            UpdatePreview();
        };

        var buttonBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { keepAllButton, useAllButton, closeButton, applyButton },
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
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Der er en ny dokumentkandidat. Gennemgå indholdet og vælg, om kandidaten skal anvendes.",
                                TextWrapping = TextWrapping.Wrap,
                            },
                            CreateHeaderRow(),
                            rows,
                            new TextBlock
                            {
                                Text = "Preview af den valgte dokumentkandidat",
                                FontWeight = FontWeight.Bold,
                                Margin = new Thickness(0, 10, 0, 0),
                            },
                            preview,
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
            ColumnDefinitions = new ColumnDefinitions("145,190,95,135,*,*,*,210"),
            ColumnSpacing = 8,
        };
        row.Children.Add(CreateValueBlock("Person", 0, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Feltsti", 1, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Handling", 2, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Årsag", 3, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Dokument", 4, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Godkendt", 5, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Ny GEDCOM", 6, FontWeight.Bold));
        row.Children.Add(CreateValueBlock("Valg", 7, FontWeight.Bold));
        return row;
    }

    private static string ActionText(BiographyStructuredDifference? difference) => difference?.Kind switch
    {
        BiographyDifferenceKind.Added => "Tilføj",
        BiographyDifferenceKind.Removed => "Fjern",
        BiographyDifferenceKind.Changed => "Ændr",
        _ => "Ændr",
    };

    private static string CauseText(BiographyStructuredDifference? difference)
    {
        if (difference is null)
        {
            return "GEDCOM";
        }

        var causes = new List<string>();
        if (difference.Causes.HasFlag(BiographyDifferenceCause.Gedcom))
        {
            causes.Add("GEDCOM");
        }

        if (difference.Causes.HasFlag(BiographyDifferenceCause.Template))
        {
            causes.Add("Skabelon");
        }

        if (difference.Causes.HasFlag(BiographyDifferenceCause.BaselineMigration))
        {
            causes.Add("Migrering");
        }

        return string.Join(" + ", causes);
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
