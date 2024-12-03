using SQLite;
using WeatherBot.Weather.Models;

namespace WeatherBot.Weather.Database;

[Serializable]
[Table("WeatherLogs")]
public class WeatherLogEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public long UserId { get; set; }
    public DateTime DateTimeUtc { get; set; }
    public WeatherReportType Type { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Payload { get; set; } = default!;
}