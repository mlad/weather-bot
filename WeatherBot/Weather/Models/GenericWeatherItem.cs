namespace WeatherBot.Weather.Models;

public class GenericWeatherItem
{
    public required DateTimeOffset Time { get; init; }
    public required string WeatherName { get; init; }
    public required string? WeatherIcon { get; init; }
    public int? Humidity { get; init; }
    public double? Visibility { get; init; }
    public required Dictionary<int, double> Temperature { get; init; }
    public required Dictionary<int, double> WindSpeed { get; init; }
    public required Dictionary<int, double> WindGusts { get; init; }
}