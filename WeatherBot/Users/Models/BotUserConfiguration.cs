using WeatherBot.Weather.Models;

namespace WeatherBot.Users.Models;

[Serializable]
public class BotUserConfiguration
{
    public WeatherReportType DefaultReportType { get; set; } = WeatherReportType.OpenWeatherMapHourly;
    public int DefaultRequestsQuota { get; set; } = 5;

    public void Validate()
    {
        if (DefaultRequestsQuota < 0)
            throw new Exception($"Users: {nameof(DefaultRequestsQuota)} cannot be negative");
    }
}