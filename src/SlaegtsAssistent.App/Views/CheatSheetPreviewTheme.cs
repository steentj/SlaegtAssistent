using Avalonia;
using Avalonia.Styling;
using System;

namespace SlaegtsAssistent.App.Views;

internal static class CheatSheetPreviewTheme
{
    public static string Apply(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        if (Application.Current?.ActualThemeVariant != ThemeVariant.Dark)
        {
            return html;
        }

        return html
            .Replace("background:#FFFFFF", "background:#1D242C", StringComparison.Ordinal)
            .Replace("color:#23313a", "color:#EDF2F7", StringComparison.Ordinal)
            .Replace("color:#174a5b", "color:#70C4D6", StringComparison.Ordinal)
            .Replace("color:#4d626a", "color:#AAB7C4", StringComparison.Ordinal)
            .Replace("border-left:4px solid #6e9eaa", "border-left:4px solid #70C4D6", StringComparison.Ordinal)
            .Replace("border:1px solid #9aaeb5", "border:1px solid #35404C", StringComparison.Ordinal)
            .Replace("background:#edf2f3", "background:#252E38", StringComparison.Ordinal);
    }
}
