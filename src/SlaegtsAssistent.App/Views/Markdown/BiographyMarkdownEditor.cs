using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views.Markdown;

public sealed class BiographyMarkdownEditor : TextEditor
{
    public static readonly StyledProperty<bool> ShowMarkdownProperty =
        AvaloniaProperty.Register<BiographyMarkdownEditor, bool>(nameof(ShowMarkdown));

    public bool ShowMarkdown
    {
        get => GetValue(ShowMarkdownProperty);
        set => SetValue(ShowMarkdownProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextEditor);

    private readonly MarkdownEditorImages _images;
    private readonly Cursor _imageCursor = new(StandardCursorType.Hand);
    private readonly Dictionary<int, List<LineDecoration>> _lines = [];
    private IReadOnlyList<MarkdownElement> _elements = [];
    private HashSet<MarkdownElement> _active = [];
    private ITextSourceVersion? _version;
    private EditorViewModel? _model;
    private bool _synchronizing;
    private bool _refreshPending;
    private bool _redrawAll;
    private bool _projectionEnabled;
    private TextView View => TextArea.TextView;

    public BiographyMarkdownEditor()
    {
        Options.EnableHyperlinks = false;
        Options.EnableEmailHyperlinks = false;
        Options.HighlightCurrentLine = false;
        Options.AllowScrollBelowDocument = false;
        Options.EnableRectangularSelection = false;
        TextArea.ContextMenu = CreateEditMenu();
        _images = new MarkdownEditorImages(() => ScheduleRefresh(redrawAll: true));
        View.ElementGenerators.Add(new MarkdownElements(this));
        View.LineTransformers.Add(new MarkdownFormatting(this));
        TextChanged += HandleTextChanged;
        TextArea.Caret.PositionChanged += (_, _) => ScheduleRefresh();
        TextArea.SelectionChanged += (_, _) => ScheduleRefresh();
        TextArea.GotFocus += (_, _) => ScheduleRefresh();
        TextArea.LostFocus += (_, _) => ScheduleRefresh();
        View.SizeChanged += (_, args) =>
        {
            if (args.WidthChanged && _elements.Any(element => element.Image is not null))
                ScheduleRefresh(redrawAll: true);
        };
        ActualThemeVariantChanged += (_, _) => ScheduleRefresh(redrawAll: true);
        AddHandler(KeyDownEvent, (_, _) => FlushRefresh(), RoutingStrategies.Tunnel);
    }

    internal void Attach(EditorViewModel? model)
    {
        if (ReferenceEquals(_model, model)) return;
        if (_model is not null) _model.PropertyChanged -= HandleModelChanged;
        _model = model;
        _images.Clear();
        SetImageRoots();
        _synchronizing = true;
        try
        {
            Document = new TextDocument(model?.MarkdownText ?? string.Empty);
        }
        finally
        {
            _synchronizing = false;
        }
        if (model is not null) model.PropertyChanged += HandleModelChanged;
        ScheduleRefresh(redrawAll: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowMarkdownProperty)
        {
            if (!ShowMarkdown) _images.RetryFailures();
            ScheduleRefresh(redrawAll: true);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _images.Clear();
        ScheduleRefresh(redrawAll: true);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new BiographyEditorAutomationPeer(this);

    private void HandleTextChanged(object? sender, EventArgs e)
    {
        ScheduleRefresh();
        if (!_synchronizing && _model is not null && _model.MarkdownText != Text)
            _model.MarkdownText = Text;
    }

    private void HandleModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_model is null) return;
        if (e.PropertyName == nameof(EditorViewModel.AllowedPreviewRoots))
        {
            SetImageRoots();
            ScheduleRefresh(redrawAll: true);
        }
        else if (e.PropertyName == nameof(EditorViewModel.MarkdownText) && Text != _model.MarkdownText)
        {
            Text = _model.MarkdownText;
        }
    }

    private void SetImageRoots() =>
        _images.SetDocument(_model?.DocumentPath, _model?.AllowedPreviewRoots ?? []);

    private ContextMenu CreateEditMenu()
    {
        var commands = new (string Label, RelayCommand Command)[]
        {
            ("Fortryd", new(() => Undo(), () => CanUndo)),
            ("Gentag", new(() => Redo(), () => CanRedo)),
            ("Klip", new(Cut, () => CanCut)),
            ("Kopiér", new(Copy, () => CanCopy)),
            ("Indsæt", new(Paste, () => CanPaste)),
            ("Markér alt", new(SelectAll, () => CanSelectAll))
        };
        var menu = new ContextMenu
        {
            ItemsSource = commands.Select(item => new MenuItem { Header = item.Label, Command = item.Command }).ToArray()
        };
        menu.Opening += (_, _) =>
        {
            foreach (var (_, command) in commands) command.NotifyCanExecuteChanged();
        };
        return menu;
    }

    private void ScheduleRefresh(bool redrawAll = false)
    {
        _redrawAll |= redrawAll;
        if (_refreshPending) return;
        _refreshPending = true;
        Dispatcher.UIThread.Post(FlushRefresh, DispatcherPriority.Render);
    }

    private void FlushRefresh()
    {
        if (!_refreshPending) return;
        _refreshPending = false;

        var projectionEnabled = !ShowMarkdown;
        if (!ReferenceEquals(_version, Document.Version) || _projectionEnabled != projectionEnabled)
        {
            _version = Document.Version;
            _projectionEnabled = projectionEnabled;
            BuildLineDecorations();
            _redrawAll = true;
        }

        var selection = TextArea.Selection.SurroundingSegment;
        var active = ShowMarkdown ? [] : _elements.Where(element =>
            (TextArea.IsFocused && element.Range.Contains(CaretOffset)) ||
            (selection is { Length: > 0 } && element.Range.Overlaps(selection.Offset, selection.EndOffset)))
            .ToHashSet();

        var previous = _active;
        _active = active;
        if (_redrawAll)
        {
            _redrawAll = false;
            View.Redraw();
            return;
        }

        previous.SymmetricExceptWith(active);
        foreach (var element in previous)
            View.Redraw(element.Range.Start, element.Range.Length);
    }

    private void BuildLineDecorations()
    {
        _lines.Clear();
        _elements = _projectionEnabled ? MarkdownEditorProjection.Parse(Text) : [];
        if (_projectionEnabled)
            _images.Retain(_elements.Select(element => element.Image?.Source).OfType<string>());
        foreach (var element in _elements)
        {
            foreach (var decoration in element.Decorations)
                AddToLines(element, decoration);
            if (element.Image is not null)
                AddToLines(element, new MarkdownDecoration(element.Range, MarkdownAppearance.Plain, string.Empty));
        }
        foreach (var parts in _lines.Values)
            parts.Sort((left, right) => left.Part.Range.Start.CompareTo(right.Part.Range.Start));
    }

    private void AddToLines(MarkdownElement element, MarkdownDecoration decoration)
    {
        var range = decoration.Range;
        for (var line = Document.GetLineByOffset(range.Start);
             line is not null && line.Offset < range.End; line = line.NextLine)
        {
            // Bevar linjeskift og kildepositioner ved formatering på tværs af linjer.
            var part = new MarkdownRange(Math.Max(line.Offset, range.Start), Math.Min(line.EndOffset, range.End));
            if (part.Length <= 0) continue;

            if (!_lines.TryGetValue(line.LineNumber, out var list))
                _lines.Add(line.LineNumber, list = []);
            list.Add(new(element, decoration with { Range = part }, part.Start == range.Start));
        }
    }

    private IEnumerable<LineDecoration> GetDecorations(DocumentLine line) =>
        ShowMarkdown || line.Length > VisualLine.LENGTH_LIMIT || !ReferenceEquals(_version, Document.Version) ||
        !_lines.TryGetValue(line.LineNumber, out var list)
            ? []
            : list;

    private Control CreateImage(Bitmap bitmap, MarkdownImage image, int offset)
    {
        var availableWidth = View.Bounds.Width > 0 ? View.Bounds.Width : Math.Max(120, Bounds.Width - 24);
        var scale = Math.Min(1, Math.Min(availableWidth * image.WidthFraction / bitmap.Size.Width, 420 / bitmap.Size.Height));
        var control = new Image
        {
            Source = bitmap,
            Width = bitmap.Size.Width * scale,
            Height = bitmap.Size.Height * scale,
            Cursor = _imageCursor
        };
        AutomationProperties.SetName(control, image.AlternativeText.Length > 0 ? image.AlternativeText : "Billede");
        ToolTip.SetTip(control, "Klik for at redigere billedets Markdown");
        control.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                return;
            e.Handled = true;
            TextArea.Focus();
            Select(offset, 0);
        };
        return control;
    }

    private IBrush? GetBrush(string name) =>
        this.TryFindResource(name, ActualThemeVariant, out var resource) ? resource as IBrush : null;

    private sealed record LineDecoration(MarkdownElement Element, MarkdownDecoration Part, bool FirstPart);

    private sealed class BiographyEditorAutomationPeer(BiographyMarkdownEditor editor)
        : ControlAutomationPeer(editor), IValueProvider
    {
        public bool IsReadOnly => editor.IsReadOnly;

        public string Value => editor.Text;

        public void SetValue(string? value)
        {
            if (!editor.IsEnabled || IsReadOnly)
                throw new InvalidOperationException("Biografiteksten kan ikke redigeres.");
            value ??= string.Empty;
            if (value != editor.Text)
                editor.Document.Replace(0, editor.Document.TextLength, value);
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

        protected override bool HasKeyboardFocusCore() => editor.TextArea.IsFocused;

        protected override void SetFocusCore() => editor.TextArea.Focus();
    }

    private sealed class MarkdownElements(BiographyMarkdownEditor editor) : VisualLineElementGenerator
    {
        private LineDecoration[] _parts = [];

        public override void StartGeneration(ITextRunConstructionContext context)
        {
            base.StartGeneration(context);
            var parts = editor.GetDecorations(context.VisualLine.FirstDocumentLine);
            foreach (var item in parts)
                if (item.FirstPart && item.Element.Image is { } image)
                    editor._images.Find(image.Source);
            _parts = parts.Where(item => item.Part.Replacement is not null && !editor._active.Contains(item.Element)).ToArray();
        }

        public override int GetFirstInterestedOffset(int startOffset) =>
            _parts.FirstOrDefault(item => item.Part.Range.Start >= startOffset)?.Part.Range.Start ?? -1;

        public override VisualLineElement? ConstructElement(int offset)
        {
            var item = _parts.First(part => part.Part.Range.Start == offset);
            var length = item.Part.Range.Length;
            if (item.Element.Image is { } image)
            {
                if (editor._images.Find(image.Source) is not { } bitmap) return null;
                if (item.FirstPart)
                    return new InlineObjectElement(length, editor.CreateImage(bitmap, image, item.Element.Range.Start + 2));
            }
            var text = item.FirstPart ? item.Part.Replacement : string.Empty;
            return new FormattedTextElement(string.IsNullOrEmpty(text) ? "\u200B" : text, length);
        }
    }

    private sealed class MarkdownFormatting(BiographyMarkdownEditor editor) : DocumentColorizingTransformer
    {
        private static readonly FontFamily Monospace = new("Consolas, Menlo, DejaVu Sans Mono, monospace");

        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var item in editor.GetDecorations(line))
            {
                var part = item.Part;
                if (part.Appearance != MarkdownAppearance.Plain && !editor._active.Contains(item.Element))
                    ChangeLinePart(part.Range.Start, part.Range.End, element => Apply(element.TextRunProperties, part));
            }
        }

        private void Apply(VisualLineElementTextRunProperties properties, MarkdownDecoration part)
        {
            var typeface = properties.Typeface;
            switch (part.Appearance)
            {
                case MarkdownAppearance.Heading:
                    properties.SetFontRenderingEmSize(properties.FontRenderingEmSize * part.Scale);
                    ApplyBrush(properties, "AppAccentBrush");
                    goto case MarkdownAppearance.Bold;
                case MarkdownAppearance.Bold:
                    properties.SetTypeface(new Typeface(typeface.FontFamily, typeface.Style, FontWeight.Bold, typeface.Stretch));
                    break;
                case MarkdownAppearance.Italic:
                    properties.SetTypeface(new Typeface(typeface.FontFamily, FontStyle.Italic, typeface.Weight, typeface.Stretch));
                    break;
                case MarkdownAppearance.Strike:
                    properties.SetTextDecorations(TextDecorations.Strikethrough);
                    break;
                case MarkdownAppearance.Highlight:
                    properties.SetBackgroundBrush(editor.GetBrush("AppAccentMutedBrush"));
                    break;
                case MarkdownAppearance.Inserted:
                    properties.SetTextDecorations(TextDecorations.Underline);
                    break;
                case MarkdownAppearance.Superscript:
                case MarkdownAppearance.Subscript:
                    properties.SetFontRenderingEmSize(properties.FontRenderingEmSize * 0.75);
                    properties.SetBaselineAlignment(part.Appearance == MarkdownAppearance.Superscript
                        ? BaselineAlignment.Superscript : BaselineAlignment.Subscript);
                    break;
                case MarkdownAppearance.Code:
                    properties.SetBackgroundBrush(editor.GetBrush("AppPanelMutedBrush"));
                    goto case MarkdownAppearance.Table;
                case MarkdownAppearance.Table:
                    properties.SetTypeface(new Typeface(Monospace, typeface.Style, typeface.Weight, typeface.Stretch));
                    break;
                case MarkdownAppearance.Link:
                    properties.SetTextDecorations(TextDecorations.Underline);
                    ApplyBrush(properties, "AppAccentBrush");
                    break;
                case MarkdownAppearance.Muted:
                    ApplyBrush(properties, "AppMutedTextBrush");
                    break;
            }
        }

        private void ApplyBrush(VisualLineElementTextRunProperties properties, string name)
        {
            if (editor.GetBrush(name) is { } brush) properties.SetForegroundBrush(brush);
        }
    }
}
