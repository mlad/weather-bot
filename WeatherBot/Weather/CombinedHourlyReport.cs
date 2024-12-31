using ImageMagick;
using ImageMagick.Drawing;
using WeatherBot.Resources;
using WeatherBot.Users;
using WeatherBot.Weather.Models;

namespace WeatherBot.Weather;

public sealed class CombinedHourlyReport : IDisposable
{
    private const int MaxHours = 12;

    private const double TimeColWidth = 100;
    private const double ColWidth = 190;
    private const double RowHeight = 30;
    private const double PadSide = 5;

    private const double FontSize = 16;
    private const double FontHeight = 21;

    private static readonly MagickColor BackgroundColor = new(245, 245, 245);
    private static readonly MagickColor TextColor = new(100, 100, 100);
    private static readonly MagickColor RowColor1 = new(255, 255, 255);
    private static readonly MagickColor RowColor2 = new(230, 230, 230);

    private static readonly Dictionary<WeatherReportType, string> ColumnNames = new()
    {
        { WeatherReportType.OpenMeteoHourly, "Open-Meteo" },
        { WeatherReportType.OpenWeatherMapHourly, "OpenWeatherMap" },
        { WeatherReportType.AccuWeatherHourly, "AccuWeather" }
    };

    private readonly Dictionary<string, MagickImage> _icons = new();
    private readonly BotUser _user;

    public static byte[] Generate(BotUser user, WeatherLog[] responses)
    {
        using var report = new CombinedHourlyReport(user);
        return report.GenerateInternal(responses);
    }

    private byte[] GenerateInternal(WeatherLog[] responses)
    {
        var rowWidth = TimeColWidth + ColWidth * responses.Length;

        using var image = new MagickImage(
            color: BackgroundColor,
            width: (uint)(rowWidth + PadSide),
            height: (uint)(RowHeight * (MaxHours + 1) + 5)
        );

        DrawHeader(image, responses);

        var startTime = DateTimeOffset.UtcNow.ToOffset(responses.First().Response.UtcOffset).Hour();
        var enumerators = responses.Select(x => x.Response.Items.GetEnumerator()).ToList();

        for (var idx = 0; idx < MaxHours; idx++)
        {
            var time = startTime.AddHours(idx);
            var top = 30 + idx * RowHeight;

            var draw = new Drawables()
                // Background
                .FillColor(idx % 2 != 0 ? RowColor1 : RowColor2)
                .Rectangle(PadSide, top, rowWidth, top + RowHeight)
                // Time
                .FontPointSize(FontSize)
                .Font(Resource.Font.RobotoRegular)
                .FillColor(TextColor)
                .Text(PadSide * 2, top + FontHeight, _user.Translate("Weather:CombinedHourly:Time", time));

            for (var i = 0; i < responses.Length; i++)
            {
                var enumerator = enumerators[i];

                while (true)
                {
                    var diff = enumerator.Current?.Time.CompareTo(time);

                    if (diff is -1 or null)
                    {
                        enumerator.MoveNext();
                        continue;
                    }

                    if (diff == 1)
                    {
                        break;
                    }

                    DrawColumn(enumerator.Current!, TimeColWidth + ColWidth * i, top, draw);
                    enumerator.MoveNext();
                    break;
                }
            }

            draw.Draw(image);
        }

        return image.ToByteArray(MagickFormat.Png);
    }

    private void DrawColumn(GenericWeatherItem item, double left, double top, IDrawables<byte> draw)
    {
        var icon = GetIcon(item);
        if (icon != null)
        {
            draw.Composite(left, top + RowHeight / 2.0 - icon.Height / 2.0, CompositeOperator.SrcOver, icon);
            left = left + icon.Width + 5;
        }

        var temp = (int)Math.Round(item.Temperature.MinBy(x => x.Key).Value);
        var wind = item.WindSpeed.MinBy(x => x.Key).Value;
        var windLevel = WindLevel.Get(wind);

        draw.Text(left, top + FontHeight, _user.Translate("Weather:CombinedHourly:Temp", temp))
            .Text(left + 60, top + FontHeight, _user.Translate("Weather:CombinedHourly:Wind", wind, windLevel));
    }

    private void DrawHeader(MagickImage image, WeatherLog[] responses)
    {
        var draw = new Drawables()
            .FontPointSize(FontSize)
            .Font(Resource.Font.RobotoBold)
            .FillColor(TextColor)
            .Text(PadSide * 2, 20, _user.Translate("Weather:CombinedHourly:TimeHeader"));

        for (var i = 0; i < responses.Length; i++)
        {
            draw.Text(TimeColWidth + ColWidth * i, 20, ColumnNames[responses[i].Request.Type]);
        }

        draw.Draw(image);
    }

    private MagickImage? GetIcon(GenericWeatherItem item)
    {
        var path = item.GetIconPath();

        if (path == null)
            return null;

        if (_icons.TryGetValue(path, out var icon))
            return icon;

        icon = new MagickImage(File.ReadAllBytes(path));
        _icons[path] = icon;

        if (icon.Height != 16)
        {
            icon.Resize((uint)(icon.Width / (icon.Height / 16.0)), 16);
        }

        return icon;
    }

    public void Dispose()
    {
        foreach (var icon in _icons.Values)
        {
            icon.Dispose();
        }

        _icons.Clear();
    }

    private CombinedHourlyReport(BotUser user)
    {
        _user = user;
    }
}