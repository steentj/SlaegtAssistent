using System.Threading.Tasks;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public interface ISettingsDialogService
{
    Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings);

    Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings, Person? previewPerson)
    {
        return EditSettingsAsync(currentSettings);
    }
}
