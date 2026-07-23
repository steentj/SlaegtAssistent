using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface ISettingsDialogService
{
    Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings);
}
