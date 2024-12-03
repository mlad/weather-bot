using System.Text.Json;
using CoordinateSharp;
using WeatherBot.Weather.Database;
using WeatherBot.Weather.Models;

namespace WeatherBot.Weather;

[Serializable]
public class WeatherLog
{
    public int Id { get; }
    public long UserId { get; }
    public DateTime DateTimeUtc { get; }
    public WeatherParams Request { get; }
    public GenericWeatherResponse Response { get; }

    public static WeatherLog? TryGet(int id)
    {
        var entity = App.Database.Find<WeatherLogEntity>(id);
        return entity != null ? new WeatherLog(entity) : null;
    }

    public static WeatherLog? TryGet(WeatherParams request)
    {
        var minDate = DateTimeOffset.UtcNow.AddMinutes(-App.Config.Weather.CacheTimeoutMinutes);

        var entities = App.Database.Table<WeatherLogEntity>()
            .Where(x => x.Type == request.Type && x.DateTimeUtc > minDate)
            .OrderByDescending(x => x.Id)
            .ToList();

        var coordinate = new Coordinate(request.Lat, request.Lon);
        var maxDistance = App.Config.Weather.CacheDistanceThresholdMeters;

        var entity = entities.FirstOrDefault(x =>
            coordinate.Get_Distance_From_Coordinate(new Coordinate(x.Lat, x.Lon)).Meters <= maxDistance);

        return entity != null ? new WeatherLog(entity) : null;
    }

    public static int Count(long userId, TimeSpan inSpan)
    {
        var minDate = DateTime.UtcNow.Subtract(inSpan);
        return App.Database.Table<WeatherLogEntity>().Count(x => x.UserId == userId && x.DateTimeUtc >= minDate);
    }

    public static WeatherLog Create(long userId, WeatherParams request, GenericWeatherResponse response)
    {
        var entity = new WeatherLogEntity
        {
            UserId = userId,
            DateTimeUtc = DateTime.UtcNow,
            Type = request.Type,
            Lat = request.Lat,
            Lon = request.Lon,
            Payload = JsonSerializer.Serialize(response)
        };

        App.Database.Insert(entity);
        return new WeatherLog(entity, response);
    }

    private WeatherLog(WeatherLogEntity entity)
    {
        Id = entity.Id;
        UserId = entity.UserId;
        DateTimeUtc = entity.DateTimeUtc;
        Request = new WeatherParams(entity.Type, entity.Lat, entity.Lon);
        Response = JsonSerializer.Deserialize<GenericWeatherResponse>(entity.Payload)!;
    }

    private WeatherLog(WeatherLogEntity entity, GenericWeatherResponse response)
    {
        Id = entity.Id;
        UserId = entity.UserId;
        DateTimeUtc = entity.DateTimeUtc;
        Request = new WeatherParams(entity.Type, entity.Lat, entity.Lon);
        Response = response;
    }
}