using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public interface ITemplateEditorPreviewService
{
    Task<TemplateEditorPreviewResult> RenderAsync(
        BiographyTemplateEditorModel model,
        Person person,
        Submitter? submitter,
        CancellationToken cancellationToken);
}

public sealed record TemplateEditorPreviewResult(
    string Markdown,
    SafeMarkdownPreviewResult SafePreview);
