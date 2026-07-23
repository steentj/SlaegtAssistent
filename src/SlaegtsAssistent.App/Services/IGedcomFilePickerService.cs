using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface IGedcomFilePickerService
{
    Task<string?> PickGedcomFileAsync(string? suggestedStartFolder);
}
