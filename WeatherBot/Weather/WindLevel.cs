namespace WeatherBot.Weather;

public static class WindLevel
{
    // Reference: https://en.wikipedia.org/wiki/Beaufort_scale
    private static readonly double[] SpeedMaxValues = [0.3, 1.6, 4, 6, 8, 11, 14, 18, 21, 25, 29, 33, double.MaxValue];

    public static int Get(double speed)
    {
        return Array.FindIndex(SpeedMaxValues, x => speed < x);
    }
}