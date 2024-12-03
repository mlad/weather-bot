using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WeatherBot.Weather.Models;

public record WeatherParams(WeatherReportType Type, double Lat, double Lon)
{
    public static bool TryParse(string[] args, int offset, [NotNullWhen(true)] out WeatherParams? result)
    {
        if (!WeatherReportTypeExtensions.TryParse(args.ElementAtOrDefault(offset), out var type) ||
            !double.TryParse(args.ElementAtOrDefault(offset + 1), CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(args.ElementAtOrDefault(offset + 2), CultureInfo.InvariantCulture, out var lon))
        {
            result = null;
            return false;
        }

        result = new WeatherParams(type, lat, lon);
        return true;
    }

    public WeatherParams With(WeatherReportType type) => this with { Type = type };

    public override string ToString()
    {
        return $"{Type.GetKey()} {Lat} {Lon}";
    }
}