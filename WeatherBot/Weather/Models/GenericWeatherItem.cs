using WeatherBot.Resources;
using WeatherBot.Text;

namespace WeatherBot.Weather.Models;

public class GenericWeatherItem
{
    public required DateTimeOffset Time { get; init; }
    public required string WeatherName { get; init; }
    public required GenericWeatherType WeatherType { get; init; }
    public int? Humidity { get; init; }
    public double? Visibility { get; init; }
    public required Dictionary<int, double> Temperature { get; init; }
    public required Dictionary<int, double> WindSpeed { get; init; }
    public required Dictionary<int, double> WindGusts { get; init; }

    public string? GetIconEmoji() => WeatherType switch
    {
        GenericWeatherType.Clear => Emoji.Sun,
        GenericWeatherType.FewClouds => Emoji.SunSmallCloud,
        GenericWeatherType.ScatteredClouds => Emoji.SunBigCloud,
        GenericWeatherType.BrokenClouds => Emoji.Cloud, // no suitable emoji
        GenericWeatherType.OvercastClouds => Emoji.Cloud,
        GenericWeatherType.Rain => Emoji.Rain,
        GenericWeatherType.Thunderstorm => Emoji.Thunderstorm,
        GenericWeatherType.Snow => Emoji.Snow,
        GenericWeatherType.Fog => Emoji.Fog,
        GenericWeatherType.Unknown => null,
        _ => throw new ArgumentOutOfRangeException()
    };

    public string? GetIconPath() => WeatherType switch
    {
        GenericWeatherType.Clear => Resource.Emoji.Sun,
        GenericWeatherType.FewClouds => Resource.Emoji.SunSmallCloud,
        GenericWeatherType.ScatteredClouds => Resource.Emoji.SunBigCloud,
        GenericWeatherType.BrokenClouds => Resource.Emoji.Cloud, // TODO: make separate icon
        GenericWeatherType.OvercastClouds => Resource.Emoji.Cloud,
        GenericWeatherType.Rain => Resource.Emoji.Rain,
        GenericWeatherType.Thunderstorm => Resource.Emoji.Thunderstorm,
        GenericWeatherType.Snow => Resource.Emoji.Snow,
        GenericWeatherType.Fog => Resource.Emoji.Fog,
        GenericWeatherType.Unknown => null,
        _ => throw new ArgumentOutOfRangeException()
    };
}

public enum GenericWeatherType
{
    Unknown,
    Clear,
    FewClouds, // 11-25%
    ScatteredClouds, // 25-50%
    BrokenClouds, // 51-84%
    OvercastClouds, // 85-100%
    Rain,
    Thunderstorm,
    Snow,
    Fog
}