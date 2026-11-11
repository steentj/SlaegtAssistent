using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SlaegtsAssistent.App.Services;

public sealed class GraphSettingsStore
{
    private readonly IAtomicFileWriter _writer;
    public GraphSettingsStore(IAtomicFileWriter writer) => _writer = writer;

    public string? Load(string? workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace)) return null;
        var path = Path.Combine(Path.GetFullPath(workspace), ".slaegtsassistent", "graph-settings.json");
        if (!File.Exists(path)) return null;
        try
        {
            var settings = JsonSerializer.Deserialize(File.ReadAllText(path), AppJsonContext.Default.GraphSettings);
            if (settings is null || settings.FormatVersion != 1) throw new InvalidDataException("Grafens indstillinger har en ukendt version.");
            return settings.ProbandId;
        }
        catch (JsonException exception) { throw new InvalidDataException("Grafens indstillinger er ugyldige.", exception); }
    }

    public void Save(string workspace, string? probandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);
        var path = Path.Combine(Path.GetFullPath(workspace), ".slaegtsassistent", "graph-settings.json");
        var json = JsonSerializer.Serialize(new GraphSettings(1, probandId), AppJsonContext.Default.GraphSettings);
        _writer.WriteText(path, json, new UTF8Encoding(false));
    }
}
