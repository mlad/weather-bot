using SQLite;
using WeatherBot.Weather.Models;

namespace WeatherBot.Users.Database;

[Table("Users")]
public class BotUserEntity
{
    [PrimaryKey] public long Id { get; init; }
    [MaxLength(32)] public string? Name { get; init; }
    [MaxLength(2)] public string Language { get; init; } = default!;
    public WeatherReportType DefaultWeatherType { get; init; }
    public int RequestsQuota { get; init; }
}