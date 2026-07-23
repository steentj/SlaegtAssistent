namespace SlaegtsAssistent.App.Services;

public interface IApplicationSettingsService
{
    AppSettings Load();

    void Save(AppSettings settings);
}
