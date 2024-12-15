using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace WeatherBot.Text;

public static class Translator
{
    public const string FallbackLanguage = "en";
    public static readonly string[] AllLanguages;

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new();
    private static readonly Dictionary<string, CultureInfo> Cultures = new();

    static Translator()
    {
        Strings["en"] = ReadLanguageResource("WeatherBot.Text.Translations.lang_en.json");
        Strings["ru"] = ReadLanguageResource("WeatherBot.Text.Translations.lang_ru.json");

        AllLanguages = Strings.Keys.Order().ToArray();

        foreach (var language in AllLanguages)
        {
            Cultures[language] = CultureInfo.CreateSpecificCulture(language);
        }

        foreach (var key in Strings.SelectMany(x => x.Value).Select(x => x.Key).Distinct())
        {
            foreach (var (lang, strings) in Strings)
            {
                if (!strings.ContainsKey(key))
                    Console.WriteLine($"[WARNING] Language '{lang}' missing '{key}' translation");
            }
        }
    }

    public static string Get(string lang, string key)
    {
        if (!Strings.TryGetValue(lang, out var strings))
        {
            strings = Strings[FallbackLanguage];
        }

        if (!strings.TryGetValue(key, out var result))
        {
            throw new ApplicationException($"String '{key}' is not translated to '{lang}'");
        }

        return result;
    }

    public static string Format(string lang, string key, params object?[] args)
    {
        return string.Format(Get(lang, key), args);
    }

    public static CultureInfo GetCulture(string lang)
    {
        return Cultures[lang];
    }

    private static Dictionary<string, string> ReadLanguageResource(string path)
    {
        var result = new Dictionary<string, string>();
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(path);
        Process(JsonSerializer.Deserialize<JsonElement>(stream!));
        return result;

        void Process(JsonElement element, string? key = null)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in element.EnumerateObject())
                {
                    Process(item.Value, key != null ? $"{key}:{item.Name}" : item.Name);
                }
            }
            else
            {
                result[key!] = element.ToString();
            }
        }
    }
}