using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class SafeMarkdownPreviewServiceTests
{
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
