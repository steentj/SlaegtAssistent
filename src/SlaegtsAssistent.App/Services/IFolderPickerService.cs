using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(string title, string? suggestedStartFolder);
}
