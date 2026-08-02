using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface ITemplateFilePickerService
{
    Task<string?> PickTemplateFileAsync(string? suggestedStartFolder);
}
