using System.Globalization;

namespace SlaegtsAssistent.Core.Biography;

internal static class NoteTimestampFormatter
{
    public static string Format(string timestamp)
    {
        var fields = timestamp.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length is not (3 or 4)
            || !DateTime.TryParseExact(string.Join(' ', fields.Take(3)), ["d MMM yyyy", "dd MMM yyyy"],
                CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date))
        {
            return timestamp;
        }

        var hasTime = fields.Length == 4;
        if (hasTime)
        {
            if (!TimeSpan.TryParse(fields[3], CultureInfo.InvariantCulture, out var time)
                || time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
            {
                return timestamp;
            }
            date = date.Add(time);
        }

        var culture = CultureInfo.CurrentCulture;
        if (culture.Equals(CultureInfo.InvariantCulture) || culture.TwoLetterISOLanguageName == "da")
        {
            return date.ToString(hasTime ? "dd-MM-yyyy HH:mm" : "dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        return date.ToString(hasTime ? "g" : "d", culture);
    }
}
