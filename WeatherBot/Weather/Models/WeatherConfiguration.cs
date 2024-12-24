namespace WeatherBot.Weather.Models;

[Serializable]
public class WeatherConfiguration
{
    // Requests logging and caching
    public int LogRetentionPeriodDays { get; set; } = 14;
    public int CacheLifetimeMinutes { get; set; } = 5;
    public double CacheDistanceThresholdMeters { get; set; } = 500;

    // Format - multiple heights
    public int MultiHeightItemsPerPage { get; set; } = 3;

    // Format - hourly
    public int HourlyDaysPerPage { get; set; } = 3;
    public int HourlyItemsPerDay { get; set; } = 8;

    // Format - daily
    public int DailyItemsPerPage { get; set; } = 14;

    public void Validate()
    {
        if (LogRetentionPeriodDays < 1)
            throw new Exception($"Weather: {nameof(LogRetentionPeriodDays)} must be positive");

        if (CacheLifetimeMinutes < 0)
            throw new Exception($"Weather: {nameof(CacheLifetimeMinutes)} cannot be negative");

        if (CacheDistanceThresholdMeters < 0)
            throw new Exception($"Weather: {nameof(CacheDistanceThresholdMeters)} cannot be negative");

        if (MultiHeightItemsPerPage < 1)
            throw new Exception($"Weather: {nameof(MultiHeightItemsPerPage)} must be positive");

        if (HourlyDaysPerPage < 1)
            throw new Exception($"Weather: {nameof(HourlyDaysPerPage)} must be positive");

        if (HourlyItemsPerDay is < 1 or > 24)
            throw new Exception($"Weather: {nameof(HourlyItemsPerDay)} must be between 1 and 24");

        if (DailyItemsPerPage < 1)
            throw new Exception($"Weather: {nameof(DailyItemsPerPage)} must be positive");
    }
}