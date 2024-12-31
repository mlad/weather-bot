using WeatherBot.Geocoding.Database;

namespace WeatherBot.Geocoding;

[Serializable]
public class GeocodingLog
{
    public static int Count(long userId, TimeSpan inSpan)
    {
        var minDate = DateTime.UtcNow.Subtract(inSpan);
        return App.Database.Table<GeocodingLogEntity>().Count(x => x.UserId == userId && x.DateTimeUtc >= minDate);
    }

    public static void Create(long userId, string query)
    {
        var entity = new GeocodingLogEntity
        {
            Id = 0,
            UserId = userId,
            DateTimeUtc = DateTime.UtcNow,
            Query = query
        };

        App.Database.Insert(entity);
    }
}