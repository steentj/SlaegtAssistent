using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public class EditorViewModelTests
{
    [Fact]
    public void PreviewHtml_ShouldReflectMarkdownText()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore());

        viewModel.MarkdownText = "# Hej";

        viewModel.PreviewHtml.Should().Contain("<h1>Hej</h1>");
    }

    [Fact]
    public void PreviewHtml_ShouldUpdate_WhenMarkdownTextChanges()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore());

        viewModel.MarkdownText = "# Første";
        viewModel.MarkdownText = "# Anden";

        viewModel.PreviewHtml.Should().Contain("<h1>Anden</h1>");
        viewModel.PreviewHtml.Should().NotContain("Første");
    }

    [Fact]
    public void Load_ShouldPopulateMarkdownText_FromStore()
    {
        const string path = "/tmp/person.md";
        var fileStore = new RecordingMarkdownFileStore
        {
            ReadResult = "# Test",
        };
        var viewModel = new EditorViewModel(path, fileStore);

        viewModel.Load();

        fileStore.LastReadPath.Should().Be(path);
        viewModel.MarkdownText.Should().Be("# Test");
        viewModel.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void MarkdownText_ShouldMarkEditorDirty_AfterLoad()
    {
        const string path = "/tmp/person.md";
        var fileStore = new RecordingMarkdownFileStore
        {
            ReadResult = "# Test",
        };
        var viewModel = new EditorViewModel(path, fileStore);

        viewModel.Load();
        viewModel.MarkdownText = "# Redigeret";

        viewModel.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Save_ShouldWriteMarkdownText_ToStore()
    {
        const string path = "/tmp/person.md";
        var fileStore = new RecordingMarkdownFileStore();
        var viewModel = new EditorViewModel(path, fileStore)
        {
            MarkdownText = "## Gem mig",
        };

        viewModel.SaveCommand.Execute(null);

        fileStore.LastWritePath.Should().Be(path);
        fileStore.LastWriteContent.Should().Be("## Gem mig");
        viewModel.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Save_WhenStorageFails_ShouldRemainDirtyAndPropagateTheError()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new FailingMarkdownFileStore())
        {
            MarkdownText = "Brugerens ændring",
        };

        var action = () => viewModel.SaveCommand.Execute(null);

        action.Should().Throw<IOException>().WithMessage("Simuleret skrivefejl.");
        viewModel.IsDirty.Should().BeTrue();
        viewModel.MarkdownText.Should().Be("Brugerens ændring");
    }

    [Fact]
    public void Save_ShouldPersistVisibleFactChangesToFrontMatter()
    {
        const string path = "/tmp/person.md";
        var metadata = new BiographyDocumentMetadata(
            1,
            "@I1@",
            "Anna Jensen",
            new BiographyFactsSnapshot("Anna Jensen", "F", "12 MAR 1900", "Aarhus", null, null, []));
        var fileStore = new RecordingMarkdownFileStore
        {
            ReadResult = BiographyDocumentSerializer.Serialize(
                metadata,
                "# Anna Jensen\n\n## Fakta\n- **Født:** 12 MAR 1900 i Aarhus\n\n## Biografi\nTekst.\n"),
        };
        var viewModel = new EditorViewModel(path, fileStore);

        viewModel.Load();
        viewModel.MarkdownText = viewModel.MarkdownText.Replace("12 MAR 1900", "13 MAR 1900");
        viewModel.SaveCommand.Execute(null);

        var savedDocument = BiographyDocumentParser.Parse(fileStore.LastWriteContent!);
        savedDocument.Metadata!.Facts.BirthDate.Should().Be("13 MAR 1900");
    }

    [Fact]
    public void PreviewHtml_ShouldBeEmpty_ForEmptyMarkdown()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore())
        {
            MarkdownText = string.Empty,
        };

        viewModel.PreviewHtml.Should().BeEmpty();
    }

    [Fact]
    public void PreviewMode_ShouldDefaultToWeb()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore());

        viewModel.PreviewMode.Should().Be(PreviewMode.Web);
        viewModel.IsWebPreviewSelected.Should().BeTrue();
        viewModel.IsHtmlPreviewSelected.Should().BeFalse();
    }

    [Fact]
    public void PreviewMode_ShouldSwitchBetweenWebAndHtml()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore());

        viewModel.IsHtmlPreviewSelected = true;

        viewModel.PreviewMode.Should().Be(PreviewMode.Html);
        viewModel.IsHtmlPreviewSelected.Should().BeTrue();
        viewModel.IsWebPreviewSelected.Should().BeFalse();
    }

    [Fact]
    public void PreviewWebUri_ShouldContainRenderedHtml()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore())
        {
            MarkdownText = "# Hej",
        };

        viewModel.PreviewWebUri.Should().NotBeNull();
        viewModel.PreviewWebUri.Scheme.Should().Be("data");
        viewModel.PreviewWebUri.ToString().Should().Contain("data:text/html");
        viewModel.PreviewWebUri.ToString().Should().Contain("Hej");
    }

    [Fact]
    public void PreviewHtmlDocument_ShouldContainRenderedHtml()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore())
        {
            MarkdownText = "# Hej",
        };

        viewModel.PreviewHtmlDocument.Should().Contain("<h1>Hej</h1>");
    }

    [Fact]
    public void PreviewHtmlDocument_ShouldDefineThemeableTextAndBackgroundColors()
    {
        var viewModel = new EditorViewModel("/tmp/person.md", new RecordingMarkdownFileStore())
        {
            MarkdownText = "# Hej",
        };

        viewModel.PreviewHtmlDocument.Should().Contain("color:#23313a");
        viewModel.PreviewHtmlDocument.Should().Contain("background:#FFFFFF");
    }

    private sealed class RecordingMarkdownFileStore : IMarkdownFileStore
    {
        public string ReadResult { get; set; } = string.Empty;

        public string? LastReadPath { get; private set; }

        public string? LastWritePath { get; private set; }

        public string? LastWriteContent { get; private set; }

        public string Read(string path)
        {
            LastReadPath = path;
            return ReadResult;
        }

        public void Write(string path, string content)
        {
            LastWritePath = path;
            LastWriteContent = content;
        }
    }

    private sealed class FailingMarkdownFileStore : IMarkdownFileStore
    {
        public string Read(string path) => string.Empty;

        public void Write(string path, string content)
        {
            throw new IOException("Simuleret skrivefejl.");
        }
    }
}
