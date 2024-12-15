using WeatherBot.Geocoding.Database;

namespace WeatherBot.Geocoding;

[Serializable]
public class GeocodingLog(GeocodingLogEntity entity)
{
    public int Id { get; } = entity.Id;
    public long UserId { get; } = entity.UserId;
    public DateTime DateTimeUtc { get; } = entity.DateTimeUtc;
    public string Query { get; } = entity.Query;

    public static int Count(long userId, TimeSpan inSpan)
    {
        var minDate = DateTime.UtcNow.Subtract(inSpan);
        return App.Database.Table<GeocodingLogEntity>().Count(x => x.UserId == userId && x.DateTimeUtc >= minDate);
    }

    public static GeocodingLog Create(long userId, string query)
    {
        var entity = new GeocodingLogEntity
        {
            Id = 0,
            UserId = userId,
            DateTimeUtc = DateTime.UtcNow,
            Query = query
        };

        App.Database.Insert(entity);
        return new GeocodingLog(entity);
    }
}