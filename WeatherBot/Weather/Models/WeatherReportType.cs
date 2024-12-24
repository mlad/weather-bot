namespace WeatherBot.Weather.Models;

public enum WeatherReportType
{
    // Open Weather Map
    OpenWeatherMapHourly,

    // Open Meteo
    OpenMeteoDaily,
    OpenMeteoHourly,
    OpenMeteoHourlyMultiHeight,

    // AccuWeather
    AccuWeatherHourly
}

public static class WeatherReportTypeExtensions
{
    public static readonly Dictionary<string, WeatherReportTypeDefinition> All = new()
    {
        ["owm_hourly"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.OpenWeatherMapHourly,
            Fetch = OpenWeatherMap.Get,
            Format = (weather, lang, page, now) => weather.FormatHourly(lang, page, now)
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
        },
        ["aw_hourly"] = new WeatherReportTypeDefinition
        {
            Type = WeatherReportType.AccuWeatherHourly,
            Fetch = AccuWeather.Get,
            Format = (weather, lang, page, now) => weather.FormatHourly(lang, page, now)
        }
    };

    public static IReadOnlyCollection<IReadOnlyCollection<string>> ButtonOrder
    {
        get
        {
            if (_buttonOrder == null)
            {
                var hourly = new List<string>();

                if (App.Config.OpenWeatherMap != null)
                    hourly.Add("owm_hourly");

                hourly.Add("om_hourly");

                if (App.Config.AccuWeather != null)
                    hourly.Add("aw_hourly");

                _buttonOrder = [hourly, ["om_daily", "om_heights"]];
            }

            return _buttonOrder;
        }
    }

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

    private static IReadOnlyCollection<IReadOnlyCollection<string>>? _buttonOrder;
}