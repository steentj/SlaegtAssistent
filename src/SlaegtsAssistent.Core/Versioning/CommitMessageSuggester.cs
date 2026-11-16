using System.Globalization;

namespace SlaegtsAssistent.Core.Versioning;

public static class CommitMessageSuggester
{
    public static string Suggest(DateTimeOffset now)
        => string.Format(CultureInfo.InvariantCulture, "Ændringer {0:dd-MM-yyyy HH:mm}", now.ToLocalTime());
}
