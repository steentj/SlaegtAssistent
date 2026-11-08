using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface ITemplateFilePickerService
{
    Task<string?> PickTemplateFileAsync(string? suggestedStartFolder);

    Task<string?> PickTemplateSaveFileAsync(string? suggestedPath) => Task.FromResult<string?>(null);
}
