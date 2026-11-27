using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Tests;

public sealed class SafeMarkdownPreviewServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenereretNotattabel_AfsluttesFoerMarkoerOgVisesIPreview(bool withNote)
    {
        var person = new Person("@I1@") { FullName = "Anna" };
        if (withNote)
        {
            person.Notes.Add("Et notat");
        }

        var model = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion,
        [
            new(BiographyTemplateBlockType.Notes, true, "Notater", BiographyTemplatePresentation.Table,
                [BiographyTemplateField.PersonNote, BiographyTemplateField.FamilyNote]),
        ]);
        var generator = new BiographyTemplateMarkdownGenerator(new BiographyTemplateEditorCompiler().Compile(model));
        var snapshot = CanonicalBiographySnapshot.Create(person);
        foreach (var document in new[] { generator.Generate(person), generator.Generate(snapshot, snapshot) })
        {
            var body = BiographyDocumentParser.Parse(document).Body;
            body.Should().Contain((withNote ? "|  | Person: Et notat |" : "| --- | --- |")
                + "\n\n" + BiographyGeneratedSectionMerger.EndMarker);

            var html = new SafeMarkdownPreviewService().Render(body, "/tmp/person.md").Html;
            html.Should().Contain("<table>").And.Contain("<th>Dato</th>").And.Contain("<th>Notat</th>");
            if (withNote)
            {
                html.Should().Contain("Person: Et notat");
            }
        }
    }

    [Theory]
    [InlineData(25)]
    [InlineData(50)]
    public void ValgtStoerrelseVisesProportionaltIPreview(int percent)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllBytes(Path.Combine(root, "foto.png"), [1, 2, 3]);
            var body = $"![Foto](foto.png \"SlaegtsAssistent:width={percent}\")";
            var result = new SafeMarkdownPreviewService().Render(body, Path.Combine(root, "person.md"));
            result.Html.Should().Contain($"max-width:{percent}%").And.Contain("height:auto");
            result.Html.Should().NotContain("SlaegtsAssistent:width");
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void TopbilledmarkoerIEksempelkodeForbliverSynlig()
    {
        const string body = "```markdown\n<!-- SlaegtsAssistent:portrait:start -->\n```";
        var result = new SafeMarkdownPreviewService().Render(body, "/tmp/person.md");
        result.Html.Should().Contain("SlaegtsAssistent:portrait:start");
    }

    [Fact]
    public void InterneTopbilledmarkoererVisesIkke()
    {
        const string body = "<!-- SlaegtsAssistent:portrait:start -->\n![Foto](mangler.png)\n<!-- SlaegtsAssistent:portrait:end -->\n\n# Anna";
        var result = new SafeMarkdownPreviewService().Render(body, "/tmp/person.md");
        result.Html.Should().NotContain("SlaegtsAssistent:portrait").And.Contain("Anna");
    }

    [Fact]
    public void Render_ShouldBlockAllRemoteAndActiveContentWithoutNetworkAttempts()
    {
        var markdown = """
            ![fjernbillede](https://example.invalid/foto.jpg)
            <script>fetch('https://example.invalid/data')</script>
            <iframe src="https://example.invalid/frame"></iframe>
            <link rel="stylesheet" href="https://example.invalid/site.css">
            <style>@font-face{src:url(https://example.invalid/font.woff)}</style>
            [script](javascript:alert(1))
            [data](data:text/html,skadelig)
            """;

        var result = new SafeMarkdownPreviewService().Render(markdown, "/arbejde/anna.md");

        result.HtmlDocument.Should().NotContain("src=\"https:");
        result.HtmlDocument.Should().NotMatchRegex("(?i)<script");
        result.HtmlDocument.Should().NotMatchRegex("(?i)<iframe");
        result.HtmlDocument.Should().NotMatchRegex("(?i)<link\\b");
        result.HtmlDocument.Should().NotMatchRegex("(?i)<style[^>]*>@font-face");
        result.HtmlDocument.Should().NotMatchRegex("(?i)javascript:");
        result.HtmlDocument.Should().NotMatchRegex("(?i)data:text/html");
        result.HtmlDocument.Should().Contain("Indhold blev blokeret af hensyn til privatliv og sikkerhed");
        result.NetworkRequestAttempts.Should().Be(0);
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void Render_ShouldEmbedAllowedLocalImageAndBlockPathEscape()
    {
        using var area = new TemporaryDirectory();
        var workspace = Directory.CreateDirectory(Path.Combine(area.Path, "arbejde")).FullName;
        var outside = Directory.CreateDirectory(Path.Combine(area.Path, "udenfor")).FullName;
        var allowedImage = Path.Combine(workspace, "foto.png");
        var outsideImage = Path.Combine(outside, "hemmelig.png");
        File.WriteAllBytes(allowedImage, [0x89, 0x50, 0x4e, 0x47]);
        File.WriteAllBytes(outsideImage, [0x89, 0x50, 0x4e, 0x47]);
        var markdownPath = Path.Combine(workspace, "anna.md");

        var result = new SafeMarkdownPreviewService().Render(
            "![lokal](foto.png)\n![udbrud](../udenfor/hemmelig.png)",
            markdownPath,
            [workspace]);

        result.Html.Should().Contain("src=\"data:image/png;base64,");
        result.Html.Should().Contain("Lokalt billede blev blokeret");
        result.Html.Should().NotContain("hemmelig.png");
        result.NetworkRequestAttempts.Should().Be(0);
    }

    [Fact]
    public void Render_ShouldBlockSymbolicLinkThatEscapesAllowedRoot()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var area = new TemporaryDirectory();
        var workspace = Directory.CreateDirectory(Path.Combine(area.Path, "arbejde")).FullName;
        var outside = Directory.CreateDirectory(Path.Combine(area.Path, "udenfor")).FullName;
        var outsideImage = Path.Combine(outside, "hemmelig.png");
        File.WriteAllBytes(outsideImage, [0x89, 0x50, 0x4e, 0x47]);
        File.CreateSymbolicLink(Path.Combine(workspace, "genvej.png"), outsideImage);

        var result = new SafeMarkdownPreviewService().Render(
            "![genvej](genvej.png)",
            Path.Combine(workspace, "anna.md"),
            [workspace]);

        result.Html.Should().Contain("Lokalt billede blev blokeret");
        result.Html.Should().NotContain("data:image/png;base64,");
    }

    [Theory]
    [InlineData("//eksempel.invalid/medier/foto.png")]
    [InlineData("file://eksempel.invalid/medier/foto.png")]
    [InlineData("%2F%2Feksempel.invalid/medier/foto.png")]
    public void NetworkImagePathIsRejectedAsExternal(string source)
    {
        var path = Path.Combine(Path.GetTempPath(), "person.md");
        var result = new SafeMarkdownPreviewService().Render($"![Foto]({source})", path);

        result.Html.Should().Contain("Eksternt billede blev blokeret").And.NotContain("<img");
        result.NetworkRequestAttempts.Should().Be(0);
    }

    [Fact]
    public void Render_ShouldKeepExternalLinkInertUntilNavigationPolicyConfirmsIt()
    {
        var result = new SafeMarkdownPreviewService().Render(
            "[Kilde](https://example.dk/person)",
            "/arbejde/anna.md");

        result.Html.Should().Contain("data-preview-external=\"true\"");
        result.Html.Should().Contain("https://example.dk/person");
        result.ExternalLinks.Should().ContainSingle().Which.AbsoluteUri.Should().Be("https://example.dk/person");

        var decision = PreviewNavigationPolicy.Evaluate(new Uri("https://example.dk/person"));
        decision.AllowInPreview.Should().BeFalse();
        decision.RequiresConfirmation.Should().BeTrue();
        decision.DisplayDestination.Should().Be("https://example.dk/person");
    }

    [Fact]
    public void NavigationPolicy_ShouldKeepLocalhostOutsideExternalLinkPolicy()
    {
        var destination = new Uri("http://localhost:12345/");

        var decision = PreviewNavigationPolicy.Evaluate(destination);

        decision.AllowInPreview.Should().BeFalse();
        decision.RequiresConfirmation.Should().BeTrue();
        PreviewNavigationPolicy.AllowsResource(destination).Should().BeTrue();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:text/html,test")]
    [InlineData("ftp://example.dk/file")]
    public void NavigationPolicy_ShouldDenyActiveOrUnsupportedSchemes(string destination)
    {
        var decision = PreviewNavigationPolicy.Evaluate(new Uri(destination));

        decision.AllowInPreview.Should().BeFalse();
        decision.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public void Render_ShouldPreserveMarkdownSourceAndUseRestrictiveCsp()
    {
        const string markdown = "# Anna\n\n<script>alert(1)</script>\n";
        var original = markdown;

        var result = new SafeMarkdownPreviewService().Render(markdown, "/arbejde/anna.md");

        markdown.Should().Be(original);
        result.HtmlDocument.Should().Contain("default-src 'none'");
        result.HtmlDocument.Should().Contain("connect-src 'none'");
        result.HtmlDocument.Should().Contain("frame-src 'none'");
        result.HtmlDocument.Should().Contain("font-src 'none'");
        result.HtmlDocument.Should().Contain("img-src data:");
        result.Diagnostics.Should().ContainSingle(message => message.Contains("Rå HTML"));
    }

    [Fact]
    public void MainAndHelpPreview_ShouldUseSameSecurityPolicy()
    {
        const string markdown = "<script>alert(1)</script>\n![ekstern](https://example.invalid/x.png)";
        var service = new SafeMarkdownPreviewService();

        var main = service.Render(markdown, "/arbejde/anna.md");
        var help = service.RenderHelp(markdown);

        main.HtmlDocument.Should().Contain(SafeMarkdownPreviewService.ContentSecurityPolicy);
        help.HtmlDocument.Should().Contain(SafeMarkdownPreviewService.ContentSecurityPolicy);
        main.Html.Should().NotContain("<script>");
        help.Html.Should().NotContain("<script>");
        main.NetworkRequestAttempts.Should().Be(0);
        help.NetworkRequestAttempts.Should().Be(0);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
