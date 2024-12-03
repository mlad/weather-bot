namespace WeatherBot.Weather.Models;

[Serializable]
public class WeatherConfiguration
{
    // Requests logging and caching
    public int LogRetentionPeriodDays { get; set; } = 14;
    public int CacheTimeoutMinutes { get; set; } = 5;
    public double CacheDistanceThresholdMeters { get; set; } = 500;

    // Format - multiple heights
    public int MultiHeightItemsPerPage { get; set; } = 3;

    // Format - hourly
    public int HourlyDaysPerPage { get; set; } = 3;
    public int HourlyEveryNthHour { get; set; } = 3;

    // Format - daily
    public int DailyItemsPerPage { get; set; } = 7;

    public void Validate()
    {
        if (LogRetentionPeriodDays < 1)
            throw new Exception($"Weather: {nameof(LogRetentionPeriodDays)} must be positive");

        if (CacheTimeoutMinutes < 0)
            throw new Exception($"Weather: {nameof(CacheTimeoutMinutes)} cannot be negative");

        if (CacheDistanceThresholdMeters < 0)
            throw new Exception($"Weather: {nameof(CacheDistanceThresholdMeters)} cannot be negative");

        if (MultiHeightItemsPerPage < 1)
            throw new Exception($"Weather: {nameof(MultiHeightItemsPerPage)} must be positive");

        if (HourlyDaysPerPage < 1)
            throw new Exception($"Weather: {nameof(HourlyDaysPerPage)} must be positive");

        if (HourlyEveryNthHour is < 1 or > 23)
            throw new Exception($"Weather: {nameof(HourlyEveryNthHour)} must be between 1 and 23");

        if (DailyItemsPerPage < 1)
            throw new Exception($"Weather: {nameof(DailyItemsPerPage)} must be positive");
    }
}