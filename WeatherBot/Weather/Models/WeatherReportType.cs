using WeatherBot.Users;

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
    AccuWeatherHourly,

    // Combined
    CombinedHourly
}

public static class WeatherReportTypeExtensions
{
    public static readonly Dictionary<string, BaseReportDefinition> All = new()
    {
        ["owm_hourly"] = new BasicReportDefinition
        {
            Type = WeatherReportType.OpenWeatherMapHourly,
            Fetch = OpenWeatherMap.Get,
            Format = (weather, lang, page, now) => weather.FormatHourly(lang, page, now),
            IsAvailable = () => App.Config.OpenWeatherMap != null
        },
        ["om_daily"] = new BasicReportDefinition
        {
            Type = WeatherReportType.OpenMeteoDaily,
            Fetch = (lat, lon, _) => OpenMeteo.GetDaily(lat, lon),
            Format = (weather, lang, page, _) => weather.FormatDaily(lang, page),
            IsAvailable = () => true
        },
        ["om_hourly"] = new BasicReportDefinition
        {
            Type = WeatherReportType.OpenMeteoHourly,
            Fetch = (lat, lon, _) => OpenMeteo.GetHourly(lat, lon),
            Format = (weather, lang, page, now) => weather.FormatHourly(lang, page, now),
            IsAvailable = () => true
        },
        ["om_heights"] = new BasicReportDefinition
        {
            Type = WeatherReportType.OpenMeteoHourlyMultiHeight,
            Fetch = (lat, lon, _) => OpenMeteo.GetHourlyMultiHeight(lat, lon),
            Format = (weather, lang, page, now) => weather.FormatHourlyMultiHeight(lang, page, now),
            IsAvailable = () => true
        },
        ["aw_hourly"] = new BasicReportDefinition
        {
            Type = WeatherReportType.AccuWeatherHourly,
            Fetch = AccuWeather.Get,
            Format = (weather, lang, page, now) => weather.FormatHourly(lang, page, now),
            IsAvailable = () => App.Config.AccuWeather != null
        },
        ["combined_hourly"] = new InheritedReportDefinition
        {
            Type = WeatherReportType.CombinedHourly,
            BaseTypes =
            [
                WeatherReportType.OpenWeatherMapHourly,
                WeatherReportType.OpenMeteoHourly,
                WeatherReportType.AccuWeatherHourly
            ],
            Format = CombinedHourlyReport.Generate,
            IsAvailable = () => true
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

                _buttonOrder = [hourly, ["om_daily", "om_heights", "combined_hourly"]];
            }

            return _buttonOrder;
        }
    }

    public static string GetKey(this WeatherReportType value) => TypeToKey[value];

    public static BaseReportDefinition GetDefinition(this WeatherReportType value) => All[TypeToKey[value]];

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

    private static readonly Dictionary<WeatherReportType, string> TypeToKey
        = All.ToDictionary(x => x.Value.Type, x => x.Key);

    private static IReadOnlyCollection<IReadOnlyCollection<string>>? _buttonOrder;
}

public abstract class BaseReportDefinition
{
    public required WeatherReportType Type { get; init; }
    public required Func<bool> IsAvailable { get; init; }
}

public class BasicReportDefinition : BaseReportDefinition
{
    public required FetchFunc Fetch { get; init; }
    public required FormatFunc Format { get; init; }

    public delegate Task<GenericWeatherResponse> FetchFunc(double lat, double lon, string lang);

    public delegate WeatherReportFormatResult FormatFunc(GenericWeatherResponse weather, string lang, int page, DateTime now);
}

public class InheritedReportDefinition : BaseReportDefinition
{
    public required WeatherReportType[] BaseTypes { get; init; }
    public required FormatFunc Format { get; init; }

    public delegate byte[] FormatFunc(BotUser user, WeatherLog[] responses);
}