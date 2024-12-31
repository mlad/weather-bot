using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherBot.Weather.Models;

namespace WeatherBot.Weather;

public static class OpenWeatherMap
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private const string WeatherEndpoint = "https://api.openweathermap.org/data/2.5/weather";
    private const string ForecastEndpoint = "https://api.openweathermap.org/data/2.5/forecast";

    public static async Task<GenericWeatherResponse> Get(double lat, double lon, string lang)
    {
        using var http = new HttpClient();
        var items = new List<GenericWeatherItem>();

        var current = await GetCurrent(http, lat, lon, lang);
        items.Add(current.ToGeneric());

        var hourly = await GetHourly(http, lat, lon, lang);
        items.AddRange(hourly.ToGeneric());

        return new GenericWeatherResponse
        {
            Latitude = lat,
            Longitude = lon,
            Items = items,
            UtcOffset = TimeSpan.FromSeconds(hourly.City.Timezone)
        };
    }

    private static async Task<ForecastResponse> GetHourly(HttpClient http, double lat, double lon, string lang)
    {
        using var response = await http.GetAsync(GetUrl(ForecastEndpoint, lat, lon, lang));
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<ForecastResponse>(stream, SerializerOptions)!;
    }

    private static async Task<WeatherResponse> GetCurrent(HttpClient http, double lat, double lon, string lang)
    {
        using var response = await http.GetAsync(GetUrl(WeatherEndpoint, lat, lon, lang));
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<WeatherResponse>(stream, SerializerOptions)!;
    }

    private static string GetUrl(string endpoint, double lat, double lon, string lang)
    {
        var token = App.Config.OpenWeatherMap?.ApiToken
                    ?? throw new Exception("OpenWeatherMap is not configured");

        return $"{endpoint}?lat={lat}&lon={lon}&units=metric&lang={lang}&appid={token}";
    }

    [Serializable]
    private class WeatherResponse
    {
        public required WeatherModel[] Weather { get; init; }
        public required MainModel Main { get; init; }
        public int? Visibility { get; init; }
        public required WindModel Wind { get; init; }

        public GenericWeatherItem ToGeneric() => new()
        {
            Time = DateTimeOffset.UtcNow.Hour(),
            WeatherName = Weather[0].Description,
            WeatherType = Weather[0].GetGenericWeatherType(),
            Humidity = Main.Humidity,
            Visibility = Visibility,
            Temperature = new Dictionary<int, double> { [0] = Main.Temp },
            WindSpeed = new Dictionary<int, double> { [0] = Wind.Speed },
            WindGusts = new Dictionary<int, double> { [0] = Wind.Gust }
        };
    }

    [Serializable]
    public class ForecastResponse
    {
        public required ForecastDay[] List { get; init; }
        public required CityModel City { get; init; }

        public IEnumerable<GenericWeatherItem> ToGeneric() =>
            List.Select(d => new GenericWeatherItem
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(d.TimeStamp),
                WeatherName = d.Weather[0].Description,
                WeatherType = d.Weather[0].GetGenericWeatherType(),
                Humidity = d.Main.Humidity,
                Visibility = d.Visibility,
                Temperature = new Dictionary<int, double> { [0] = d.Main.Temp },
                WindSpeed = new Dictionary<int, double> { [0] = d.Wind.Speed },
                WindGusts = new Dictionary<int, double> { [0] = d.Wind.Gust }
            });

        [Serializable]
        public class ForecastDay
        {
            [JsonPropertyName("dt")] public required int TimeStamp { get; init; }
            public required MainModel Main { get; init; }
            public required WeatherModel[] Weather { get; init; }
            public required WindModel Wind { get; init; }
            public int Visibility { get; init; }
        }

        [Serializable]
        public class CityModel
        {
            public required int Timezone { get; init; }
        }
    }

    [Serializable]
    public class WeatherModel
    {
        public required int Id { get; init; }
        public required string Main { get; init; }
        public required string Description { get; init; }

        public GenericWeatherType GetGenericWeatherType() => Main switch
        {
            "Clear" => GenericWeatherType.Clear,
            "Clouds" => Id switch
            {
                801 => GenericWeatherType.FewClouds,
                802 => GenericWeatherType.ScatteredClouds,
                803 => GenericWeatherType.BrokenClouds,
                804 => GenericWeatherType.OvercastClouds,
                _ => throw new Exception($"OWM: unexpected weather id ({Id})")
            },
            "Drizzle" => GenericWeatherType.Rain,
            "Rain" => GenericWeatherType.Rain,
            "Thunderstorm" => GenericWeatherType.Thunderstorm,
            "Snow" => GenericWeatherType.Snow,
            "Atmosphere" => GenericWeatherType.Fog,
            _ => throw new Exception($"OWM: unexpected weather group ({Main})")
        };
    }

    [Serializable]
    public class MainModel
    {
        public double Temp { get; init; }
        public int Humidity { get; init; }
    }

    [Serializable]
    public class WindModel
    {
        public double Speed { get; init; }
        public double Gust { get; init; }
    }
}

[Serializable]
public class OpenWeatherMapConfiguration
{
    public required string ApiToken { get; init; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(ApiToken))
            throw new Exception($"OpenWeatherMap: {nameof(ApiToken)} must be specified");
    }
}