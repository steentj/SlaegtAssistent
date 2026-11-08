using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateEditorPreviewTests
{
    [Fact]
    public async Task Constructor_ShouldRenderLocalExampleWithoutGedcom()
    {
        var viewModel = CreateViewModel();

        await viewModel.PreviewUpdateTask;

        viewModel.PreviewPeople.Should().ContainSingle(option => option.IsExample);
        viewModel.SelectedPreviewPerson!.IsExample.Should().BeTrue();
        viewModel.PreviewMarkdown.Should().Contain("# Anna Marie Jensen");
        viewModel.PreviewMarkdown.Should().Contain("Det lokale slægtsarkiv");
        viewModel.PreviewHtmlDocument.Should().Contain(SafeMarkdownPreviewService.ContentSecurityPolicy);
        viewModel.PreviewNetworkRequestAttempts.Should().Be(0);
    }

    [Fact]
    public async Task SelectedPreviewPerson_ShouldSwitchBetweenExampleAndLoadedPerson()
    {
        var person = new Person("@I99@")
        {
            FullName = "Bodil Sørensen",
            BirthDate = "2 FEB 1920",
        };
        var viewModel = CreateViewModel([person]);
        await viewModel.PreviewUpdateTask;

        viewModel.SelectedPreviewPerson = viewModel.PreviewPeople.Single(option => !option.IsExample);
        await viewModel.PreviewUpdateTask;

        viewModel.PreviewMarkdown.Should().Contain("# Bodil Sørensen");
        viewModel.PreviewMarkdown.Should().NotContain("Anna Marie Jensen");

        viewModel.SelectedPreviewPerson = viewModel.PreviewPeople.Single(option => option.IsExample);
        await viewModel.PreviewUpdateTask;
        viewModel.PreviewMarkdown.Should().Contain("# Anna Marie Jensen");
    }

    [Fact]
    public async Task ModelChanges_ShouldAutomaticallyRefreshPreview()
    {
        var viewModel = CreateViewModel();
        await viewModel.PreviewUpdateTask;
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Sources);

        viewModel.SelectedBlockIsVisible = false;
        await viewModel.PreviewUpdateTask;

        viewModel.PreviewMarkdown.Should().NotContain("## Kilder");
    }

    [Fact]
    public async Task LatestResult_ShouldWinWhenAnOlderRenderFinishesLast()
    {
        var service = new DelayedPreviewService();
        var viewModel = CreateViewModel(previewService: service);
        await viewModel.PreviewUpdateTask;
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);

        viewModel.SelectedBlockHeading = "Langsom";
        var slow = viewModel.PreviewUpdateTask;
        viewModel.SelectedBlockHeading = "Nyeste";
        var latest = viewModel.PreviewUpdateTask;
        await Task.WhenAll(slow, latest);

        viewModel.PreviewMarkdown.Should().Be("Nyeste");
    }

    [Fact]
    public async Task Failure_ShouldKeepLastValidPreviewAndShowDanishError()
    {
        var service = new FailingAfterFirstPreviewService();
        var viewModel = CreateViewModel(previewService: service);
        await viewModel.PreviewUpdateTask;
        var validMarkdown = viewModel.PreviewMarkdown;
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);

        viewModel.SelectedBlockHeading = "Ugyldig";
        await viewModel.PreviewUpdateTask;

        viewModel.PreviewMarkdown.Should().Be(validMarkdown);
        viewModel.PreviewErrorMessage.Should().Contain("kunne ikke opdateres");
        viewModel.PreviewErrorMessage.Should().Contain("Testfejl");
    }

    [Fact]
    public async Task ProductionService_ShouldMatchTheProductionRendererAndRemainOffline()
    {
        var example = BiographyTemplateExampleData.Create();
        var model = BiographyTemplateEditorModel.CreateDefault();
        var service = new TemplateEditorPreviewService();

        var result = await service.RenderAsync(model, example.Person, example.Submitter, CancellationToken.None);
        var source = new BiographyTemplateEditorCompiler().Compile(model);
        var expected = new BiographyTemplateRenderer().Render(
            new BiographyTemplateLoader().Parse(source),
            BiographyTemplateContext.FromPerson(example.Person, example.Submitter));

        result.Markdown.Should().Be(expected);
        result.SafePreview.NetworkRequestAttempts.Should().Be(0);
        result.SafePreview.HtmlDocument.Should().Contain("script-src 'none'");
    }

    private static TemplateEditorViewModel CreateViewModel(
        IEnumerable<Person>? people = null,
        ITemplateEditorPreviewService? previewService = null) =>
        new(
            BiographyTemplateEditorModel.CreateDefault(),
            previewPeople: people,
            previewService: previewService,
            previewDelay: TimeSpan.Zero);

    private sealed class DelayedPreviewService : ITemplateEditorPreviewService
    {
        public async Task<TemplateEditorPreviewResult> RenderAsync(
            BiographyTemplateEditorModel model,
            Person person,
            Submitter? submitter,
            CancellationToken cancellationToken)
        {
            var heading = model.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts).Heading;
            await Task.Delay(heading == "Langsom" ? 80 : 5);
            return Result(heading);
        }
    }

    private sealed class FailingAfterFirstPreviewService : ITemplateEditorPreviewService
    {
        private int _calls;

        public Task<TemplateEditorPreviewResult> RenderAsync(
            BiographyTemplateEditorModel model,
            Person person,
            Submitter? submitter,
            CancellationToken cancellationToken)
        {
            _calls++;
            return _calls == 1
                ? Task.FromResult(Result("Gyldig"))
                : Task.FromException<TemplateEditorPreviewResult>(
                    new BiographyTemplateEditorException("Testfejl i blokken Fakta."));
        }
    }

    private static TemplateEditorPreviewResult Result(string markdown)
    {
        var safe = new SafeMarkdownPreviewService().RenderHelp(markdown);
        return new TemplateEditorPreviewResult(markdown, safe);
    }
}
