using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherBot.Text;
using WeatherBot.Weather.Models;

namespace WeatherBot.Weather;

public static class OpenWeatherMap
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private const string WeatherEndpoint = "https://api.openweathermap.org/data/2.5/weather";
    private const string ForecastEndpoint = "https://api.openweathermap.org/data/2.5/forecast";

    public static async Task<GenericWeatherResponse> GetCurrent(double lat, double lon, string lang)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(GetUrl(WeatherEndpoint, lat, lon, lang));
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<WeatherResponse>(stream, SerializerOptions)!.ToGeneric();
    }

    public static async Task<GenericWeatherResponse> GetHourly(double lat, double lon, string lang)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(GetUrl(ForecastEndpoint, lat, lon, lang));
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<ForecastResponse>(stream, SerializerOptions)!.ToGeneric();
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
        public required int Timezone { get; init; }
        [JsonPropertyName("coord")] public required CoordinatesModel Coordinates { get; init; }

        public GenericWeatherResponse ToGeneric() => new()
        {
            Latitude = Coordinates.Lat,
            Longitude = Coordinates.Lon,
            Items =
            [
                new GenericWeatherItem
                {
                    Time = DateTimeOffset.UtcNow,
                    WeatherName = Weather[0].Description,
                    WeatherIcon = Weather[0].GetEmoji(),
                    Humidity = Main.Humidity,
                    Visibility = Visibility,
                    Temperature = new Dictionary<int, double> { [0] = Main.Temp },
                    WindSpeed = new Dictionary<int, double> { [0] = Wind.Speed },
                    WindGusts = new Dictionary<int, double> { [0] = Wind.Gust }
                }
            ],
            UtcOffset = TimeSpan.FromSeconds(Timezone)
        };
    }

    [Serializable]
    public class ForecastResponse
    {
        public required ForecastDay[] List { get; init; }
        public required CityModel City { get; init; }

        public GenericWeatherResponse ToGeneric() => new()
        {
            Latitude = City.Coordinates.Lat,
            Longitude = City.Coordinates.Lon,
            Items = List.Select(d => new GenericWeatherItem
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(d.TimeStamp),
                WeatherName = d.Weather[0].Description,
                WeatherIcon = d.Weather[0].GetEmoji(),
                Humidity = d.Main.Humidity,
                Visibility = d.Visibility,
                Temperature = new Dictionary<int, double> { [0] = d.Main.Temp },
                WindSpeed = new Dictionary<int, double> { [0] = d.Wind.Speed },
                WindGusts = new Dictionary<int, double> { [0] = d.Wind.Gust }
            }).ToList(),
            UtcOffset = TimeSpan.FromSeconds(City.Timezone)
        };

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
            [JsonPropertyName("coord")] public required CoordinatesModel Coordinates { get; init; }
        }
    }

    [Serializable]
    public class WeatherModel
    {
        public required string Description { get; init; }
        public required string Icon { get; init; }

        public string GetEmoji() => Icon[..2] switch
        {
            "01" => Emoji.ClearSky, // Clear sky
            "02" => Emoji.FewClouds, // Few clouds
            "03" => Emoji.ScatteredClouds, // Scattered clouds
            "04" => Emoji.BrokenClouds, // Broken clouds
            "09" => Emoji.Rain, // Shower rain
            "10" => Emoji.Rain, // Rain
            "11" => Emoji.Thunderstorm, // Thunderstorm
            "13" => Emoji.Snow, // Snow
            "50" => Emoji.Fog, // Mist
            _ => string.Empty
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

    [Serializable]
    public record CoordinatesModel(double Lat, double Lon);
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