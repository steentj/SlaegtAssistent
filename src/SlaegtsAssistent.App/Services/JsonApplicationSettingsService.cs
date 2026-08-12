using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlaegtsAssistent.App.Services;

public sealed class JsonApplicationSettingsService : IApplicationSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _settingsFilePath;
    private readonly IAtomicFileWriter _atomicFileWriter;

    public JsonApplicationSettingsService()
        : this(CreateDefaultSettingsFilePath(), new AtomicFileWriter())
    {
    }

    internal JsonApplicationSettingsService(string settingsFilePath)
        : this(settingsFilePath, new AtomicFileWriter())
    {
    }

    public JsonApplicationSettingsService(IAtomicFileWriter atomicFileWriter)
        : this(CreateDefaultSettingsFilePath(), atomicFileWriter)
    {
    }

    public JsonApplicationSettingsService(
        string settingsFilePath,
        IAtomicFileWriter atomicFileWriter)
    {
        _settingsFilePath = settingsFilePath ?? throw new ArgumentNullException(nameof(settingsFilePath));
        _atomicFileWriter = atomicFileWriter ?? throw new ArgumentNullException(nameof(atomicFileWriter));
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(_settingsFilePath);
        return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Kunne ikke bestemme mappe til indstillingsfil.");
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        _atomicFileWriter.WriteText(_settingsFilePath, json, new UTF8Encoding(false));
    }

    private static string CreateDefaultSettingsFilePath()
    {
        var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataDirectory, "SlaegtsAssistent", "settings.json");
    }
}
