namespace WeatherBot.Text;

public static class Emoji
{
    public const string Snow = "\u2744\ufe0f"; // ❄️
    public const string ClearSky = "\u2600\ufe0f"; // ☀️
    public const string FewClouds = "\ud83c\udf24"; // 🌤
    public const string Cloud = "\u2601\ufe0f"; // ☁️
    public const string BrokenClouds = "\ud83c\udf25"; // 🌥
    public const string Thunderstorm = "\u26c8"; // ⛈
    public const string Fog = "\ud83c\udf2b"; // 🌫
    public const string Rain = "\ud83c\udf27"; // 🌧
    public const string Drizzle = "\ud83c\udf26"; // 🌦
    public const string Rock = "\ud83e\udea8"; // 🪨
    public const string Ice = "\ud83e\uddca"; // 🧊
    public const string Hot = "\ud83e\udd75"; // 🥵
    public const string Cold = "\ud83e\udd76"; // 🥶
    public const string Wind = "\ud83d\udca8"; // 💨
    public const string Moon = "\ud83c\udf15"; // 🌕

    public const string Star = "\u2b50\ufe0f"; // ⭐️
    public const string TrashBin = "\ud83d\uddd1"; // 🗑
    public const string Refresh = "\ud83d\udd04"; // 🔄
    public const string TwistedArrows = "\ud83d\udd00"; // 🔀
    public const string Pin = "\ud83d\udccd"; // 📍
    public const string Cog = "\u2699\ufe0f"; // ⚙️
    public const string Info = "\u2139"; // ℹ️

    public static string CountryFlag(string code) =>
        string.Concat(code.ToUpper().Select(x => char.ConvertFromUtf32(x + 0x1F1A5)));
}