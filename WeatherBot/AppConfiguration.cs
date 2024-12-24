using System.Text.Json;
using WeatherBot.Bookmarks.Models;
using WeatherBot.Users.Models;
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
    public BotUserConfiguration Users { get; init; } = new();
    public WeatherConfiguration Weather { get; init; } = new();
    public BookmarkConfiguration Bookmarks { get; init; } = new();
    public OpenWeatherMapConfiguration? OpenWeatherMap { get; init; }
    public AccuWeatherConfiguration? AccuWeather { get; init; }

    public static AppConfiguration Initialize()
    {
        AppConfiguration? configuration;

        if (File.Exists(FileName))
        {
            using var f = File.OpenRead(FileName);
            configuration = JsonSerializer.Deserialize<AppConfiguration>(f);
        }
        else
        {
            configuration = new AppConfiguration { TelegramBotToken = string.Empty };

            using var f = File.Create(FileName);
            JsonSerializer.Serialize(f, configuration, SerializerOptions);
        }

        try
        {
            if (configuration == null)
                throw new Exception("Failed to deserialize configuration");

            configuration.Validate();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Environment.Exit(1);
        }

        return configuration;
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(TelegramBotToken))
            throw new Exception($"Configuration: {nameof(TelegramBotToken)} must be specified");

        try
        {
            Users.Validate();
            Weather.Validate();
            Bookmarks.Validate();
            OpenWeatherMap?.Validate();
            AccuWeather?.Validate();
        }
        catch (Exception ex)
        {
            throw new Exception($"Configuration -> {ex.Message}", ex);
        }
    }
}