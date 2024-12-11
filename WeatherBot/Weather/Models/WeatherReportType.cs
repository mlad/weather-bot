namespace WeatherBot.Weather.Models;

public enum WeatherReportType
{
    // Open Weather Map
    OpenWeatherMapCurrent,
    OpenWeatherMapHourly,

    // Open Meteo
    OpenMeteoCurrent,
    OpenMeteoDaily,
    OpenMeteoHourly,
    OpenMeteoHourlyMultiHeight
}

public static class WeatherReportTypeExtensions
{
    public static readonly Dictionary<string, WeatherReportTypeDefinition> All = new()
    {
        ["owm_current"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.OpenWeatherMapCurrent,
            Fetch = OpenWeatherMap.GetCurrent,
            Format = (weather, lang, _, now) => weather.FormatSingle(lang, now)
        },
        ["owm_hourly"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.OpenWeatherMapHourly,
            Fetch = OpenWeatherMap.GetHourly,
            Format = (weather, lang, page, now) => weather.FormatHourly(lang, page, now)
        },
        ["om_current"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.OpenMeteoCurrent,
            Fetch = (lat, lon, _) => OpenMeteo.GetCurrent(lat, lon),
            Format = (weather, lang, _, now) => weather.FormatSingle(lang, now)
        },
        ["om_daily"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.OpenMeteoDaily,
            Fetch = (lat, lon, _) => OpenMeteo.GetDaily(lat, lon),
            Format = (weather, lang, page, _) => weather.FormatDaily(lang, page)
        },
        ["om_hourly"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.OpenMeteoHourly,
            Fetch = (lat, lon, _) => OpenMeteo.GetHourly(lat, lon),
            Format = (weather, lang, page, now) => weather.FormatHourly(lang, page, now)
        },
        ["om_heights"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.OpenMeteoHourlyMultiHeight,
            Fetch = (lat, lon, _) => OpenMeteo.GetHourlyMultiHeight(lat, lon),
            Format = (weather, lang, page, now) => weather.FormatHourlyMultiHeight(lang, page, now)
        }
    };

    public static readonly string[][] ButtonOrder =
    [
        ["owm_current", "owm_hourly", "om_daily"],
        ["om_current", "om_hourly", "om_heights"]
    ];

    public static string GetKey(this WeatherReportType value) => TypeToKey[value];

    public static WeatherReportTypeDefinition GetDefinition(this WeatherReportType value) => All[TypeToKey[value]];

    public static bool TryParse(string? key, out WeatherReportType type)
    {
        if (key != null && All.TryGetValue(key, out var def))
        {
            type = def.Type;
            return true;
        }

        type = default;
        return false;
    }

    public class WeatherReportTypeDefinition
    {
        public required WeatherReportType Type { get; init; }
        public required FetchFunc Fetch { get; init; }
        public required FormatFunc Format { get; init; }

        public delegate Task<GenericWeatherResponse> FetchFunc(double lat, double lon, string lang);

        public delegate WeatherReportFormatResult FormatFunc(GenericWeatherResponse weather, string lang, int page, DateTime now);
    }

    private static readonly Dictionary<WeatherReportType, string> TypeToKey
        = All.ToDictionary(x => x.Value.Type, x => x.Key);
}