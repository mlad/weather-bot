using System.Text.Json;
using WeatherBot.Text;
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

        var locationKey = AccuWeatherLocationEntity.TryGet(lat, lon)?.Id.ToString();
        if (locationKey == null)
        {
            locationKey = (await GetAsync<LocationResponse>(http, $"{GeoSearchEndpoint}?q={lat},{lon}")).Key;
            AccuWeatherLocationEntity.Create(int.Parse(locationKey), lat, lon);
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
            WeatherIcon = WeatherIcon switch
            {
                1 or 2 => Emoji.ClearSky,
                3 or 4 => Emoji.FewClouds,
                5 or 6 => Emoji.BrokenClouds,
                7 or 8 => Emoji.Cloud,
                11 => Emoji.Fog,
                12 or 18 => Emoji.Rain,
                13 => Emoji.BrokenClouds + Emoji.Rain,
                14 => Emoji.FewClouds + Emoji.Rain,
                15 => Emoji.Thunderstorm,
                16 => Emoji.BrokenClouds + Emoji.Thunderstorm,
                17 => Emoji.FewClouds + Emoji.Thunderstorm,
                19 or 22 => Emoji.Snow,
                20 or 23 => Emoji.BrokenClouds + Emoji.Snow,
                21 => Emoji.FewClouds + Emoji.Snow,
                24 => Emoji.Ice,
                25 or 29 => Emoji.Rain + Emoji.Snow,
                26 => Emoji.Ice + Emoji.Rain,
                30 => Emoji.Hot,
                31 => Emoji.Cold,
                32 => Emoji.Wind,
                33 or 34 => Emoji.Moon,
                35 or 36 or 37 or 38 => Emoji.Moon + Emoji.Cloud,
                39 or 40 => Emoji.Moon + Emoji.Rain,
                41 or 42 => Emoji.Moon + Emoji.Thunderstorm,
                43 or 44 => Emoji.Moon + Emoji.Snow,
                _ => null
            },
            Humidity = RelativeHumidity,
            Visibility = Visibility.Value,
            Temperature = new Dictionary<int, double> { [0] = Temperature.Value },
            WindSpeed = new Dictionary<int, double> { [0] = Wind.Speed.Value * (5.0 / 18.0) }, // km/h to m/s
            WindGusts = new Dictionary<int, double> { [0] = WindGust.Speed.Value * (5.0 / 18.0) }, // km/h to m/s
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