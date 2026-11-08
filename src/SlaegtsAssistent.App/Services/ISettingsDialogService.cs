using System.Threading.Tasks;
using System.Collections.Generic;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public interface ISettingsDialogService
{
    Task<AppSettings?> EditSettingsAsync(
        AppSettings currentSettings,
        Person? previewPerson,
        string? gedcomFilePath,
        string? outputFolder,
        IEnumerable<Person> previewPeople) =>
        EditSettingsAsync(currentSettings, previewPerson, gedcomFilePath, outputFolder);

    Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings);

    Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings, Person? previewPerson)
    {
        return EditSettingsAsync(currentSettings);
    }

    Task<AppSettings?> EditSettingsAsync(
        AppSettings currentSettings,
        Person? previewPerson,
        string? gedcomFilePath,
        string? outputFolder)
    {
        return EditSettingsAsync(currentSettings, previewPerson);
    }
}
