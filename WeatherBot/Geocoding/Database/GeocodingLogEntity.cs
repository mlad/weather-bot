using SQLite;

namespace WeatherBot.Geocoding.Database;

[Serializable]
[Table("GeocodingLogs")]
public class GeocodingLogEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public long UserId { get; set; }
    public DateTime DateTimeUtc { get; set; }
    [MaxLength(100)] public string Query { get; set; } = default!;
}