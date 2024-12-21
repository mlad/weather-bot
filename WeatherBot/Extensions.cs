using WeatherBot.Text;

namespace WeatherBot;

public static class Extensions
{
    public static string Format(this TimeSpan time, string lang, bool withSeconds = false)
    {
        var parts = new List<string>();

        if (time.Days > 0)
            parts.Add(Translator.Format(lang, "TimeSpan:Days", time.Days));

        if (time.Hours > 0)
            parts.Add(Translator.Format(lang, "TimeSpan:Hours", time.Hours));

        if (time.Minutes > 0)
            parts.Add(Translator.Format(lang, "TimeSpan:Minutes", time.Minutes));

        if (withSeconds && time.Seconds > 0)
            parts.Add(Translator.Format(lang, "TimeSpan:Seconds", time.Seconds));

        return parts.Count != 0
            ? string.Join(" ", parts)
            : time.ToString("g");
    }

    public static DateTime Hour(this DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, 0, 0, d.Kind);

    public static DateTimeOffset Hour(this DateTimeOffset d) => new(d.Year, d.Month, d.Day, d.Hour, 0, 0, d.Offset);
}

public class UserException(string text, Exception? inner = null) : Exception(text, inner);