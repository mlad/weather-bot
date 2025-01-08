using System.Text.Json;
using WeatherBot.Weather.Database;
using WeatherBot.Weather.Models;

namespace WeatherBot.Weather;

public static class AccuWeather
{
    private const string ForecastEndpoint = "https://dataservice.accuweather.com/forecasts/v1/hourly/12hour";
    private const string GeoSearchEndpoint = "https://dataservice.accuweather.com/locations/v1/cities/geoposition/search";

    public static async Task<GenericWeatherResponse> Get(double lat, double lon, string lang)
    {
        using var http = new HttpClient();

        var locationKey = AccuWeatherLocationEntity.TryGet(lat, lon)?.LocationKey.ToString();
        if (locationKey == null)
        {
            locationKey = (await GetAsync<LocationResponse>(http, $"{GeoSearchEndpoint}?q={lat},{lon}")).Key;
            AccuWeatherLocationEntity.Create(lat, lon, int.Parse(locationKey));
        }

        var forecast = await GetAsync<List<ForecastResponseItem>>(http,
            $"{ForecastEndpoint}/{locationKey}?language={lang}&details=true&metric=true");

        return new GenericWeatherResponse
        {
            Latitude = lat,
            Longitude = lon,
            Items = forecast.Select(x => x.ToGeneric()).ToList(),
            UtcOffset = forecast[0].DateTime.Offset
        };
    }

    private static async Task<T> GetAsync<T>(HttpClient http, string endpoint)
    {
        var token = App.Config.AccuWeather?.ApiToken
                    ?? throw new Exception("AccuWeather is not configured");

        using var response = await http.GetAsync(endpoint + $"&apikey={token}");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<T>(stream)!;
    }

    [Serializable]
    public class ForecastResponseItem
    {
        public required DateTimeOffset DateTime { get; init; }
        public required int? WeatherIcon { get; init; }
        public required string IconPhrase { get; init; }
        public required DoubleValue Temperature { get; init; }
        public required WindModel Wind { get; init; }
        public required WindModel WindGust { get; init; }
        public required int RelativeHumidity { get; init; }
        public required DoubleValue Visibility { get; init; }

        public GenericWeatherItem ToGeneric() => new()
        {
            Time = DateTime,
            WeatherName = IconPhrase,
            WeatherType = WeatherIcon switch
            {
                1 or 30 or 31 or 32 or 33 => GenericWeatherType.Clear,
                2 or 34 => GenericWeatherType.FewClouds,
                3 or 5 or 35 => GenericWeatherType.ScatteredClouds,
                4 or 6 or 36 or 38 => GenericWeatherType.BrokenClouds,
                7 or 8 => GenericWeatherType.OvercastClouds,
                11 or 37 => GenericWeatherType.Fog,
                12 or 13 or 14 or 18 or 26 or 39 or 40 => GenericWeatherType.Rain,
                15 or 16 or 17 or 41 or 42 => GenericWeatherType.Thunderstorm,
                19 or 20 or 21 or 22 or 23 or 24 or 25 or 29 or 43 or 44 => GenericWeatherType.Snow,
                null => GenericWeatherType.Unknown,
                _ => throw new Exception($"AW: unexpected weather icon id ({WeatherIcon})")
            },
            Humidity = RelativeHumidity,
            Visibility = Visibility.Value,
            Temperature = new Dictionary<int, double> { [0] = Temperature.Value },
            WindSpeed = new Dictionary<int, double> { [0] = Wind.Speed.Value * (5.0 / 18.0) }, // km/h to m/s
            WindGusts = new Dictionary<int, double> { [0] = WindGust.Speed.Value * (5.0 / 18.0) } // km/h to m/s
        };

        [Serializable]
        public class DoubleValue
        {
            public double Value { get; init; }
        }

        [Serializable]
        public class WindModel
        {
            public required DoubleValue Speed { get; init; }
        }
    }

    [Serializable]
    public class LocationResponse
    {
        public required string Key { get; init; }
    }
}

[Serializable]
public class AccuWeatherConfiguration
{
    public required string ApiToken { get; init; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(ApiToken))
            throw new Exception($"AccuWeather: {nameof(ApiToken)} must be specified");
    }
}