using System.Text.RegularExpressions;

namespace SlaegtsAssistent.Core.Versioning;

public static partial class CommitIdValidator
{
    [GeneratedRegex("^[0-9a-f]{40,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex CommitRegex();

    public static bool IsValid(string? id)
        => !string.IsNullOrWhiteSpace(id) && CommitRegex().IsMatch(id);
}
