namespace SlaegtsAssistent.App.Services;

public sealed class AppSettings
{
    public string? DefaultGedcomInputFolder { get; set; }

    public string? DefaultMarkdownOutputFolder { get; set; }

    public string? DefaultMediaFolder { get; set; }

    public bool ShowDocumentMediaThumbnails { get; set; } = true;

    public string? GlobalBiographyTemplatePath { get; set; }

    public ThemePreference Theme { get; set; } = ThemePreference.System;
}
