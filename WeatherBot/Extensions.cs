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
}

public class UserException(string text, Exception? inner = null) : Exception(text, inner);