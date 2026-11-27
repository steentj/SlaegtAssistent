using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views.Markdown;

namespace SlaegtsAssistent.App.Tests;

public sealed class BiographyMarkdownEditorTests
{
    [Fact]
    public Task BoldTextFollowsCaretOnSameLineWithoutChangingBuffer() => HeadlessTestApplication.RunAsync(() =>
    {
        const string source = "Start __tekst__ slut";
        var store = new InMemoryMarkdownStore(source);
        var model = new EditorViewModel("biografi.md", store);
        model.Load();
        var editor = new BiographyMarkdownEditor();
        editor.Attach(model);
        var window = new Window { Width = 600, Height = 300, Content = editor };
        try
        {
            window.Show();
            editor.TextArea.Focus();
            editor.TextArea.IsFocused.Should().BeTrue();
            var peer = ControlAutomationPeer.CreatePeerForElement(editor);
            peer.GetAutomationControlType().Should().Be(AutomationControlType.Edit);
            peer.Should().BeAssignableTo<IValueProvider>().Which.Value.Should().Be(source);
            ReadLine(editor).OfType<FormattedTextElement>().Should().HaveCount(2);
            ReadLine(editor).Should().Contain(element => element.TextRunProperties.Typeface.Weight == FontWeight.Bold);

            editor.Select(source.IndexOf("tekst", StringComparison.Ordinal), 0);
            ReadLine(editor).OfType<FormattedTextElement>().Should().BeEmpty();
            for (var i = 0; i < "tekst__".Length; i++)
                window.KeyPress(Key.Right, RawInputModifiers.None, default, null);
            editor.CaretOffset.Should().Be(source.IndexOf(" slut", StringComparison.Ordinal));
            ReadLine(editor).OfType<FormattedTextElement>().Should().HaveCount(2);

            AssertClean();
            var view = editor.TextArea.TextView;
            var textPoint = view.GetVisualPosition(new AvaloniaEdit.TextViewPosition(1, 11), VisualYPosition.LineMiddle);
            var click = view.TranslatePoint(textPoint - view.ScrollOffset, window)!.Value;
            window.MouseDown(click, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(click, MouseButton.Left, RawInputModifiers.None);
            editor.CaretOffset.Should().BeInRange(8, 13);
            ReadLine(editor).OfType<FormattedTextElement>().Should().BeEmpty();
            AssertClean();

            editor.Select(source.Length, 0);
            window.KeyTextInput("!");
            model.MarkdownText.Should().Be(source + "!");
            editor.ShowMarkdown = true;
            ReadLine(editor).OfType<FormattedTextElement>().Should().BeEmpty();
            editor.ShowMarkdown = false;
            ReadLine(editor).OfType<FormattedTextElement>().Should().HaveCount(2);
            editor.Undo().Should().BeTrue();
            model.MarkdownText.Should().Be(source);
            editor.CanUndo.Should().BeFalse();

            model.SaveCommand.Execute(null);
            var reopened = new EditorViewModel("biografi.md", store);
            reopened.Load();
            editor.Attach(reopened);
            reopened.MarkdownText.Should().Be(source);
            ReadLine(editor).OfType<FormattedTextElement>().Should().HaveCount(2);
            reopened.IsDirty.Should().BeFalse();
        }
        finally { window.Close(); }

        return Task.CompletedTask;

        void AssertClean()
        {
            editor.Text.Should().Be(source);
            model.MarkdownText.Should().Be(source);
            model.IsDirty.Should().BeFalse();
            editor.CanUndo.Should().BeFalse();
        }
    });

    [Fact]
    public Task BackwardSelectionAndNestedEmphasisPreserveSourceOffsets() => HeadlessTestApplication.RunAsync(() =>
    {
        const string source = "Start ***både*** og ~~streg~~.\r\nSlut med `kode`.";
        var editor = new BiographyMarkdownEditor { Text = source };
        var window = new Window { Width = 600, Height = 300, Content = editor };
        try
        {
            window.Show();
            editor.TextArea.Focus();
            ReadLine(editor).OfType<FormattedTextElement>().Should().HaveCount(6);
            ReadLine(editor, 2).OfType<FormattedTextElement>().Should().HaveCount(2);

            editor.TextArea.Selection = Selection.Create(editor.TextArea, source.Length, 8);
            editor.CaretOffset = 8;
            ReadLine(editor).OfType<FormattedTextElement>().Should().BeEmpty();
            ReadLine(editor, 2).OfType<FormattedTextElement>().Should().BeEmpty();
            editor.SelectedText.Should().Be(source[8..]);

            editor.Select(0, 0);
            ReadLine(editor).OfType<FormattedTextElement>().Should().HaveCount(6);
            var nested = ReadLine(editor).Single(element => element.RelativeTextOffset == 9);
            nested.TextRunProperties.Typeface.Weight.Should().Be(FontWeight.Bold);
            nested.TextRunProperties.Typeface.Style.Should().Be(FontStyle.Italic);
            editor.Text.Should().Be(source);
            editor.CanUndo.Should().BeFalse();
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task LocalImageAppearsWhenCaretLeavesAndCanBeEditedByClick(bool useReference) => HeadlessTestApplication.RunAsync(async () =>
    {
        var root = Directory.CreateTempSubdirectory("slaegt-editor-test-");
        var editor = new BiographyMarkdownEditor { WordWrap = true };
        var window = new Window { Width = 640, Height = 400, Content = editor };
        try
        {
            var media = root.CreateSubdirectory("medier");
            using (var bitmap = new RenderTargetBitmap(new PixelSize(360, 180)))
                bitmap.Save(Path.Combine(media.FullName, "æ foto.png"), PngBitmapEncoderOptions.Default);
            const string target = "medier/%C3%A6%20foto.png \"SlaegtsAssistent:width=25\"";
            var imageSource = useReference ? "![Beskrivelse][foto]" : $"![Beskrivelse]({target})";
            var source = imageSource + "\r\n\r\nEfter.";
            if (useReference) source += $"\r\n\r\n[foto]: {target}";
            var model = new EditorViewModel(Path.Combine(root.FullName, "biografi.md"), new InMemoryMarkdownStore(source));
            model.Load();
            editor.Attach(model);

            window.Show();
            editor.TextArea.Focus();
            editor.Select(3, 0);
            ReadLine(editor).OfType<InlineObjectElement>().Should().BeEmpty();

            editor.Select(imageSource.Length, 0);
            Image? image = null;
            for (var i = 0; i < 100 && image is null; i++)
            {
                await Task.Delay(20);
                ReadLine(editor);
                image = editor.GetVisualDescendants().OfType<Image>().SingleOrDefault(item => item.Bounds.Width > 0);
            }
            image.Should().NotBeNull();
            image!.Bounds.Width.Should().BeApproximately(editor.TextArea.TextView.Bounds.Width / 4, 1);
            AutomationProperties.GetName(image!).Should().Be("Beskrivelse");
            var click = image!.TranslatePoint(new Point(image.Bounds.Width / 2, image.Bounds.Height / 2), window)!.Value;
            window.MouseDown(click, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(click, MouseButton.Left, RawInputModifiers.None);
            editor.CaretOffset.Should().BeInRange(1, imageSource.Length - 1);
            ReadLine(editor).OfType<InlineObjectElement>().Should().BeEmpty();

            editor.Select(imageSource.Length, 0);
            ReadLine(editor).OfType<InlineObjectElement>().Should().ContainSingle();
            editor.ShowMarkdown = true;
            ReadLine(editor).OfType<InlineObjectElement>().Should().BeEmpty();
            editor.ShowMarkdown = false;
            ReadLine(editor).OfType<InlineObjectElement>().Should().ContainSingle();

            model.MarkdownText.Should().Be(source);
            model.IsDirty.Should().BeFalse();
            editor.CanUndo.Should().BeFalse();
        }
        finally
        {
            window.Close();
            root.Delete(recursive: true);
        }
    });

    [Fact]
    public Task BlockFormattingAndTableCellsStayEditable() => HeadlessTestApplication.RunAsync(() =>
    {
        const string source = "# Overskrift ###\n\n> Citat\n\n- Liste\n\n~~~\nEksempel\n~~~\n\nA | B\n:-- | ---:\n**Et** | To\nMeget længere | Tre\n";
        var editor = new BiographyMarkdownEditor { Text = source };
        var window = new Window { Width = 700, Height = 500, Content = editor };
        try
        {
            window.Show();
            editor.TextArea.Focus();
            editor.Select(source.Length, 0);
            ReadLine(editor, 1).Should().Contain(element => element.TextRunProperties.FontRenderingEmSize > editor.FontSize);
            foreach (var line in new[] { 3, 5, 7, 9, 11, 12, 13, 14 })
                ReadLine(editor, line).OfType<FormattedTextElement>().Should().NotBeEmpty($"linje {line} skal have formatering");
            ReadLine(editor, 13).Should().Contain(element => element.TextRunProperties.Typeface.Weight == FontWeight.Bold);

            editor.Select(source.IndexOf("To", StringComparison.Ordinal), 0);
            ReadLine(editor, 13).OfType<FormattedTextElement>().Should().HaveCount(2);
            ReadLine(editor, 13).Should().Contain(element => element.TextRunProperties.Typeface.Weight == FontWeight.Bold);
            editor.Text.Should().Be(source);
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });

    private static IReadOnlyList<VisualLineElement> ReadLine(BiographyMarkdownEditor editor, int line = 1)
    {
        Dispatcher.UIThread.RunJobs();
        var view = editor.TextArea.TextView;
        view.EnsureVisualLines();
        return view.GetOrConstructVisualLine(editor.Document.GetLineByNumber(line)).Elements;
    }

    private sealed class InMemoryMarkdownStore(string content) : IMarkdownFileStore
    {
        public string Read(string path) => content;

        public void Write(string path, string text) => content = text;
    }
}
