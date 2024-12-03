using System.Text.Json;
using WeatherBot.Bookmarks;
using WeatherBot.Bookmarks.Models;
using WeatherBot.Weather;
using WeatherBot.Weather.Models;

namespace WeatherBot;

[Serializable]
public class AppConfiguration
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private const string FileName = "configuration.json";

    public required string TelegramBotToken { get; init; }
    public long[] AdminsIds { get; init; } = [];
    public WeatherConfiguration Weather { get; init; } = new();
    public BookmarkConfiguration Bookmarks { get; init; } = new();
    public OpenWeatherMapConfiguration? OpenWeatherMap { get; init; }

    public static AppConfiguration ReadOrCreate()
    {
        if (File.Exists(FileName))
        {
            using var f = File.OpenRead(FileName);
            return JsonSerializer.Deserialize<AppConfiguration>(f) ?? throw new Exception("Invalid configuration");
        }
        else
        {
            var configuration = new AppConfiguration { TelegramBotToken = string.Empty };

            using var f = File.Create(FileName);
            JsonSerializer.Serialize(f, configuration, SerializerOptions);

            return configuration;
        }
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(TelegramBotToken))
            throw new Exception($"Configuration: {nameof(TelegramBotToken)} must be specified");

        try
        {
            Weather.Validate();
            Bookmarks.Validate();
            OpenWeatherMap?.Validate();
        }
        catch (Exception ex)
        {
            throw new Exception($"Configuration -> {ex.Message}", ex);
        }
    }
}