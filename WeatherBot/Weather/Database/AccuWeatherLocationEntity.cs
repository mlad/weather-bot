using SQLite;

namespace WeatherBot.Weather.Database;

[Serializable]
[Table("AccuWeatherLocations")]
public class AccuWeatherLocationEntity
{
    [PrimaryKey] public int Id { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTime CreateTimeUtc { get; set; }

    public static AccuWeatherLocationEntity? TryGet(double lat, double lon)
    {
        return App.Database.FindWithQuery<AccuWeatherLocationEntity>(
            "SELECT * FROM AccuWeatherLocations WHERE abs(Lat - ?) < 0.005 AND abs(Lon - ?) < 0.005 LIMIT 1",
            lat, lon
        );
    }

    public static AccuWeatherLocationEntity Create(int id, double lat, double lon)
    {
        var entity = new AccuWeatherLocationEntity
        {
            Id = id,
            Lat = lat,
            Lon = lon,
            CreateTimeUtc = DateTime.UtcNow
        };

        App.Database.Insert(entity);
        return entity;
    }
}