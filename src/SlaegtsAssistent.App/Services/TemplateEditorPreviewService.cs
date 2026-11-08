using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public sealed class TemplateEditorPreviewService : ITemplateEditorPreviewService
{
    private readonly SafeMarkdownPreviewService _safePreviewService;

    public TemplateEditorPreviewService(SafeMarkdownPreviewService? safePreviewService = null)
    {
        _safePreviewService = safePreviewService ?? new SafeMarkdownPreviewService();
    }

    public Task<TemplateEditorPreviewResult> RenderAsync(
        BiographyTemplateEditorModel model,
        Person person,
        Submitter? submitter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(person);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = new BiographyTemplateEditorCompiler().Compile(model);
            cancellationToken.ThrowIfCancellationRequested();
            var template = new BiographyTemplateLoader().Parse(source);
            var markdown = new BiographyTemplateRenderer().Render(
                template,
                BiographyTemplateContext.FromPerson(person, submitter));
            cancellationToken.ThrowIfCancellationRequested();
            var documentPath = Path.Combine(
                Path.GetTempPath(),
                "slaegtsassistent-skabelon-preview.md");
            var safePreview = _safePreviewService.Render(markdown, documentPath, []);
            cancellationToken.ThrowIfCancellationRequested();
            return new TemplateEditorPreviewResult(markdown, safePreview);
        }, cancellationToken);
    }
}
