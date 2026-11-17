using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
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

    public AvaloniaGedcomDifferenceDialogService(IClassicDesktopStyleApplicationLifetime applicationLifetime) =>
        _applicationLifetime = applicationLifetime;

    public Task<IReadOnlyDictionary<string, bool>?> ShowAsync(IReadOnlyList<GedcomDifferenceReviewItem> differences)
    {
        var owner = _applicationLifetime.MainWindow;
        if (owner is null || differences.Count == 0)
            return Task.FromResult<IReadOnlyDictionary<string, bool>?>(null);

        var dialog = new GedcomDifferenceReviewWindow(differences);
        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, bool>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.Closed += (_, _) => completion.TrySetResult(dialog.Decision);
        dialog.Show(owner);
        return completion.Task;
    }
}

public sealed class GedcomDifferenceReviewWindow : Window
{
    private readonly GedcomDifferenceReviewViewModel _state;
    private readonly bool _templateOnly;
    private readonly ListBox _people;
    private readonly StackPanel _details = new() { Spacing = 12 };
    private readonly TextBlock _personHeading = new() { FontSize = 18, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _count = new();
    private readonly TextBlock _candidateStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBox _preview = new()
    {
        IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
        Height = 200, FontFamily = new FontFamily("monospace"),
    };
    private readonly Expander _previewExpander = new() { Header = "Vis dokumentkandidat" };
    public IReadOnlyDictionary<string, bool>? Decision { get; private set; }

    public GedcomDifferenceReviewWindow(IReadOnlyList<GedcomDifferenceReviewItem> differences)
    {
        _state = new GedcomDifferenceReviewViewModel(differences);
        _templateOnly = differences.Count > 0 && differences.All(item => item.StructuredDifference?.Causes == BiographyDifferenceCause.Template);
        Title = _templateOnly ? "Skabelonændringer i dokumenterne" : "Gennemgå GEDCOM-ændringer";
        Width = 1100;
        Height = 760;
        MinWidth = 900;
        MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _people = new ListBox { Name = "ReviewPeople", ItemsSource = _state.People };
        AutomationProperties.SetName(_people, "Personer med ændringer");
        _people.ItemTemplate = new FuncDataTemplate<GedcomDifferencePerson>((person, _) =>
        {
            var text = new TextBlock { Text = person?.DisplayText, TextWrapping = TextWrapping.Wrap };
            if (person is not null) ToolTip.SetTip(text, person.FilePath);
            return text;
        });
        _people.SelectionChanged += (_, _) =>
        {
            _state.SelectedPerson = _people.SelectedItem as GedcomDifferencePerson;
            ShowSelectedPerson();
        };

        var search = new TextBox { Name = "ReviewFilter", PlaceholderText = "Søg person eller fil" };
        AutomationProperties.SetName(search, "Søg blandt personer med ændringer");
        search.TextChanged += (_, _) =>
        {
            var selected = _state.SelectedPerson;
            var visible = _state.FilterPeople(search.Text);
            _people.ItemsSource = visible;
            _people.SelectedItem = selected is not null && visible.Contains(selected) ? selected : visible.FirstOrDefault();
            if (visible.Count == 0) { _state.SelectedPerson = null; ShowSelectedPerson(); }
        };

        var personColumn = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), RowSpacing = 8 };
        personColumn.Children.Add(new TextBlock { Text = "PERSONER", FontWeight = FontWeight.Bold });
        Grid.SetRow(search, 1);
        Grid.SetRow(_people, 2);
        personColumn.Children.Add(search);
        personColumn.Children.Add(_people);

        _previewExpander.Content = _preview;
        AutomationProperties.SetName(_preview, "Dokumentkandidat for den valgte person");
        var content = new StackPanel { Spacing = 12, Children = { _personHeading, _candidateStatus, _details, _previewExpander } };
        var detailsScroll = new ScrollViewer
        {
            Name = "ReviewDetails", HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = content,
        };
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("230,*"), ColumnSpacing = 16 };
        columns.Children.Add(personColumn);
        Grid.SetColumn(detailsScroll, 1);
        columns.Children.Add(detailsScroll);

        var keepAll = new Button { Content = "Bevar alle dokumentværdier" };
        keepAll.Click += (_, _) => { _state.KeepAllDocumentValues(); RefreshAll(); };
        var useAll = new Button { Content = _templateOnly ? "Vælg alle skabelonændringer" : "Vælg alle nye GEDCOM-værdier" };
        useAll.Click += (_, _) => { _state.UseAllImported(); RefreshAll(); };
        var close = new Button { Name = "ReviewCancel", Content = "Luk uden ændringer", IsCancel = true };
        close.Click += (_, _) => Close();
        var apply = new Button { Name = "ReviewApply", Content = "Anvend valgte", IsDefault = true };
        apply.Click += (_, _) =>
        {
            var decisions = _state.CreateDecision();
            var blocked = _state.People.FirstOrDefault(person =>
                person.Differences.Any(item => decisions[item.Key]) &&
                person.Differences[0].CandidatePreviewFactory is { } factory && factory(decisions) is null);
            if (blocked is not null)
            {
                search.Text = string.Empty;
                _people.SelectedItem = blocked;
                ShowSelectedPerson();
                return;
            }
            Decision = decisions;
            Close();
        };
        var bulk = new WrapPanel { Orientation = Orientation.Horizontal, Children = { keepAll, useAll } };
        var buttons = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Children = { close, apply } };
        var footer = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnDefinitions = new ColumnDefinitions("*,Auto"), RowSpacing = 6 };
        footer.Children.Add(bulk);
        Grid.SetRow(_count, 1);
        footer.Children.Add(_count);
        Grid.SetColumn(buttons, 1);
        Grid.SetRowSpan(buttons, 2);
        footer.Children.Add(buttons);
        var layout = new Grid { Margin = new Thickness(18), RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 12 };
        layout.Children.Add(new TextBlock
        {
            Text = "Gennemgå ændringerne for hver person. Dine valg bevares, når du skifter person.",
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetRow(columns, 1);
        layout.Children.Add(columns);
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);
        Content = layout;
        _people.SelectedItem = _state.SelectedPerson;
        RefreshAll();
    }

    private void RefreshAll()
    {
        _count.Text = $"{_state.SelectedCount} af {_state.CreateDecision().Count} ændringer valgt";
        ShowSelectedPerson();
    }

    private void ShowSelectedPerson()
    {
        _details.Children.Clear();
        var person = _state.SelectedPerson;
        _personHeading.Text = person is null ? "Ingen personer matcher søgningen" : person.DisplayText;
        _previewExpander.IsVisible = person is not null;
        if (person is null) { _candidateStatus.Text = string.Empty; return; }
        _previewExpander.Header = $"Vis dokumentkandidat for {person.Name}";
        foreach (var difference in person.Differences)
            _details.Children.Add(CreateDifference(difference));
        UpdatePreview();
    }

    private Control CreateDifference(GedcomDifferenceReviewItem difference)
    {
        var structured = difference.StructuredDifference;
        var migration = structured?.Causes.HasFlag(BiographyDifferenceCause.BaselineMigration) == true;
        var template = structured?.Causes == BiographyDifferenceCause.Template;
        var heading = structured?.Label ?? difference.Difference.FieldName;
        var currentValue = template && _templateOnly || structured is null
            ? difference.Difference.DocumentValue : structured.DocumentValue;
        var newValue = template && _templateOnly || structured is null
            ? difference.Difference.GedcomValue : structured.ImportedValue;
        var current = ValueSection("Dokument", currentValue);
        var imported = ValueSection(template ? "Ny skabelon" : migration ? "Migrering" : "Ny GEDCOM", newValue);
        var values = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 12 };
        values.Children.Add(current);
        Grid.SetColumn(imported, 1);
        values.Children.Add(imported);
        var keep = new RadioButton { Content = "Bevar dokumentværdi", GroupName = difference.Key, IsChecked = !_state.UsesImportedValue(difference.Key) };
        var use = new RadioButton
        {
            Content = migration ? "Migrér dokumentet" : template ? "Brug ny skabelon" : "Brug ny GEDCOM-værdi",
            GroupName = difference.Key, IsChecked = _state.UsesImportedValue(difference.Key),
        };
        AutomationProperties.SetName(keep, $"{heading}: bevar dokumentværdi");
        AutomationProperties.SetName(use, $"{heading}: {use.Content}");
        keep.IsCheckedChanged += (_, _) => { if (keep.IsChecked == true) SetChoice(difference.Key, false); };
        use.IsCheckedChanged += (_, _) => { if (use.IsChecked == true) SetChoice(difference.Key, true); };
        var choices = new WrapPanel { Orientation = Orientation.Horizontal, Children = { keep, use } };
        var baseline = difference.BaselineStatus switch
        {
            BiographyBaselineStatus.Missing => "Baseline mangler",
            BiographyBaselineStatus.UnsupportedVersion => "Ukendt baselineversion",
            _ => "Baseline er tilgængelig",
        };
        var extra = new Expander
        {
            Header = "Flere oplysninger",
            Content = new StackPanel { Spacing = 4, Children =
            {
                LabelledText("Godkendt baseline", structured is null ? difference.Difference.DocumentValue : structured.ApprovedValue),
                LabelledText("Feltsti", structured?.Path ?? difference.Difference.FieldName),
                LabelledText("Handling", ActionText(structured)),
                LabelledText("Årsag", CauseText(structured)),
                new TextBlock { Text = baseline, TextWrapping = TextWrapping.Wrap },
            } },
        };
        return new StackPanel { Spacing = 6, Children =
        {
            new Separator(),
            new TextBlock { Text = heading, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap },
            new TextBlock { Text = baseline, IsVisible = difference.BaselineStatus is BiographyBaselineStatus.Missing or BiographyBaselineStatus.UnsupportedVersion, TextWrapping = TextWrapping.Wrap },
            values, choices, extra,
        } };
    }

    private void SetChoice(string key, bool selected)
    {
        _state.SetChoice(key, selected);
        _count.Text = $"{_state.SelectedCount} af {_state.CreateDecision().Count} ændringer valgt";
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var content = _state.PreviewContent;
        _preview.Text = content is null ? "Ingen kandidat anvendes med de aktuelle valg." : BiographyDocumentParser.Parse(content).Body;
        _candidateStatus.Text = content is not null ? string.Empty
            : _state.SelectedPerson?.Differences.Any(item => _state.UsesImportedValue(item.Key)) == true
                ? "Kandidaten kan ikke anvendes med disse valg. Kontrollér valg om skabelon og migrering."
                : "Ingen ændringer er valgt for denne person.";
    }

    private static StackPanel ValueSection(string label, string? value) => new()
    {
        Spacing = 3, Children =
        {
            new TextBlock { Text = label, FontWeight = FontWeight.SemiBold },
            new TextBlock { Text = string.IsNullOrWhiteSpace(value) ? "—" : value, TextWrapping = TextWrapping.Wrap },
        },
    };

    private static TextBlock LabelledText(string label, string? value) => new()
    {
        Text = $"{label}: {(string.IsNullOrWhiteSpace(value) ? "—" : value)}", TextWrapping = TextWrapping.Wrap,
    };

    private static string ActionText(BiographyStructuredDifference? difference) => difference?.Kind switch
    {
        BiographyDifferenceKind.Added => "Tilføj", BiographyDifferenceKind.Removed => "Fjern", _ => "Ændr",
    };

    private static string CauseText(BiographyStructuredDifference? difference)
    {
        if (difference is null) return "GEDCOM";
        var causes = new List<string>();
        if (difference.Causes.HasFlag(BiographyDifferenceCause.Gedcom)) causes.Add("GEDCOM");
        if (difference.Causes.HasFlag(BiographyDifferenceCause.Template)) causes.Add("Skabelon");
        if (difference.Causes.HasFlag(BiographyDifferenceCause.BaselineMigration)) causes.Add("Migrering");
        return string.Join(" + ", causes);
    }
}
