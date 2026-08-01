namespace SlaegtsAssistent.App.Services;

public sealed class AppSettings
{
    public string? DefaultGedcomInputFolder { get; set; }

    public string? DefaultMarkdownOutputFolder { get; set; }

    public ThemePreference Theme { get; set; } = ThemePreference.System;
}
