using System.Text.Json;
using System.Web;
using WeatherBot.Geocoding.Models;

namespace WeatherBot.Geocoding;

public static class OpenMeteo
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public static async Task<IReadOnlyList<GenericGeocodingResponse>> Query(string name, string lang)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync("https://geocoding-api.open-meteo.com/v1/search?" +
                                                 $"name={HttpUtility.UrlEncode(name)}&count=5&language={lang}&format=json");
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<SearchResponse>(stream, JsonOptions)!.Results.Select(x => x.ToGeneric()).ToList();
    }

    [Serializable]
    public class SearchResponse
    {
        public Item[] Results { get; set; } = [];

        [Serializable]
        public class Item
        {
            public required string Name { get; init; }
            public required string CountryCode { get; init; }
            public required double Latitude { get; init; }
            public required double Longitude { get; init; }

            public GenericGeocodingResponse ToGeneric() => new()
            {
                Name = Name,
                CountryCode = CountryCode,
                Latitude = Latitude,
                Longitude = Longitude
            };
        }
    }
}