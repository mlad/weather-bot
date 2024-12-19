using System.Globalization;
using CoordinateSharp;
using WeatherBot.Text;

namespace WeatherBot.Weather.Models;

public class GenericWeatherResponse
{
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required IReadOnlyCollection<GenericWeatherItem> Items { get; init; }
    public required TimeSpan UtcOffset { get; init; }

    public WeatherReportFormatResult FormatHourlyMultiHeight(string lang, int page, DateTime now)
    {
        var sb = new TranslatedBuilder(lang);

        var startDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        var endDate = now.Date.AddDays(1).Subtract(UtcOffset);
        if ((endDate - startDate).TotalHours < App.Config.Weather.MultiHeightItemsPerPage)
            endDate = startDate.AddHours(3);

        var filtered = Items
            .Where(x => x.Time >= startDate && x.Time < endDate)
            .ToList();

        var pageCount = (int)Math.Ceiling(filtered.Count / (double)App.Config.Weather.MultiHeightItemsPerPage);
        if (page < 0 || page >= pageCount)
            page = 0;

        foreach (var item in filtered
                     .Skip(App.Config.Weather.MultiHeightItemsPerPage * page)
                     .Take(App.Config.Weather.MultiHeightItemsPerPage))
        {
            sb.Add("Weather:MultiHeight:Time", item.Time.ToOffset(UtcOffset));

            if (item.WeatherIcon != null)
            {
                sb.AddRaw($"{item.WeatherIcon} ");
            }

            if (item.WeatherName.StartsWith('!'))
            {
                sb.AddLine(item.WeatherName[1..]);
            }
            else
            {
                sb.AddRawLine(item.WeatherName);
            }

            if (item.Humidity != null)
            {
                sb.AddLine("Weather:Generic:Humidity", item.Humidity);
            }

            if (item.Visibility != null)
            {
                sb.AddLine("Weather:Generic:Visibility", item.Visibility / 1000);
            }

            foreach (var key in item.Temperature.Keys
                         .Concat(item.WindSpeed.Keys)
                         .Concat(item.WindGusts.Keys)
                         .Distinct()
                         .Order())
            {
                sb.Add("Weather:MultiHeight:Meter", key);

                if (item.Temperature.TryGetValue(key, out var temp))
                    sb.Add("Weather:MultiHeight:Temp", Math.Round(temp));

                if (item.WindSpeed.TryGetValue(key, out var wind))
                    sb.Add("Weather:MultiHeight:Wind", wind, WindLevel.Get(wind));

                if (item.WindGusts.TryGetValue(key, out var gusts))
                    sb.Add("Weather:MultiHeight:Wind", gusts, WindLevel.Get(gusts));

                sb.AddLine();
            }

            sb.AddLine();
        }

        AppendTimeDetails(sb, lang, now);

        return new WeatherReportFormatResult(sb.ToString(), page, pageCount);
    }

    public WeatherReportFormatResult FormatHourly(string lang, int page, DateTime now)
    {
        var sb = new TranslatedBuilder(lang);

        var startDate = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        var grouped = Items
            .Where(x => x.Time >= startDate)
            .GroupBy(x => x.Time.ToOffset(UtcOffset).Date)
            .ToList();

        var offset = grouped[0].Key == now.Add(UtcOffset).Date ? 1 : 0; // 1 if contains today weather, 0 otherwise

        var pageCount = (int)Math.Ceiling((grouped.Count - offset) / (double)App.Config.Weather.HourlyDaysPerPage) + offset;
        if (page < 0 || page >= pageCount)
            page = 0;

        if (page == 0 && offset == 1)
        {
            using var iter = grouped[0].GetEnumerator();

            var idx = 0;
            foreach (var item in grouped[0])
            {
                switch (idx++)
                {
                    case 0:
                        sb.AddLine("Weather:Hourly:Now");
                        AppendSingleDetails(sb, item);
                        break;
                    case 1:
                        sb.AddLine("Weather:Hourly:Today");
                        AppendHour(item, item.Time.ToOffset(UtcOffset));
                        break;
                    default:
                        AppendHour(item, item.Time.ToOffset(UtcOffset));
                        break;
                }
            }
        }
        else
        {
            foreach (var day in grouped
                         .Skip(App.Config.Weather.HourlyDaysPerPage * (page - offset) + offset)
                         .Take(App.Config.Weather.HourlyDaysPerPage))
            {
                sb.AddLine(
                    "Weather:Hourly:Day",
                    Translator.Get(lang, $"Weekday:{day.Key.ToString("dddd", CultureInfo.InvariantCulture)}"),
                    day.Key.Day,
                    Translator.Get(lang, $"Month:{day.Key.ToString("MMMM", CultureInfo.InvariantCulture)}")
                );

                foreach (var w in day)
                {
                    var time = w.Time.ToOffset(UtcOffset);

                    if (time.Hour % App.Config.Weather.HourlyEveryNthHour != 0 && w.Time != startDate)
                        continue;

                    AppendHour(w, time);
                }

                sb.AddLine();
            }
        }

        return new WeatherReportFormatResult(sb.ToString(), page, pageCount);

        void AppendHour(GenericWeatherItem w, DateTimeOffset time)
        {
            var wind = Math.Max(w.WindSpeed.MinBy(x => x.Key).Value, w.WindGusts.MinBy(x => x.Key).Value);

            sb.AddLine(
                "Weather:Hourly:Item",
                time,
                w.WeatherIcon,
                (int)Math.Round(w.Temperature.MinBy(x => x.Key).Value),
                wind,
                WindLevel.Get(wind)
            );
        }
    }

    public WeatherReportFormatResult FormatDaily(string lang, int page)
    {
        var sb = new TranslatedBuilder(lang);

        var pageCount = (int)Math.Ceiling(Items.Count / (double)App.Config.Weather.DailyItemsPerPage);
        if (page < 0 || page >= pageCount)
            page = 0;

        foreach (var w in Items
                     .Skip(App.Config.Weather.DailyItemsPerPage * page)
                     .Take(App.Config.Weather.DailyItemsPerPage))
        {
            var time = w.Time.ToOffset(UtcOffset);
            var wind = Math.Max(w.WindSpeed.MinBy(x => x.Key).Value, w.WindGusts.MinBy(x => x.Key).Value);

            sb.AddLine(
                "Weather:Daily:Item",
                time,
                Translator.Get(lang, $"Weekday:{time.ToString("dddd", CultureInfo.InvariantCulture)}"),
                w.WeatherIcon,
                (int)Math.Round(w.Temperature.MinBy(x => x.Key).Value),
                wind,
                WindLevel.Get(wind)
            );
        }

        return new WeatherReportFormatResult(sb.ToString(), page, pageCount);
    }

    public WeatherReportFormatResult FormatSingle(string lang, DateTime now)
    {
        var sb = new TranslatedBuilder(lang);

        AppendSingleDetails(sb, Items.First());
        AppendTimeDetails(sb, lang, now);

        return new WeatherReportFormatResult(sb.ToString(), 0, 1);
    }

    private void AppendTimeDetails(TranslatedBuilder sb, string lang, DateTimeOffset now)
    {
        sb.AddLine("Weather:Generic:LocalTime", now.ToOffset(UtcOffset));

        var location = new Coordinate(Latitude, Longitude, now.UtcDateTime);

        DateTimeOffset? sunrise = location.CelestialInfo.SunRise != null
            ? new DateTimeOffset(location.CelestialInfo.SunRise.Value, TimeSpan.Zero)
            : null;

        DateTimeOffset? sunset = location.CelestialInfo.SunSet != null
            ? new DateTimeOffset(location.CelestialInfo.SunSet.Value, TimeSpan.Zero)
            : null;

        if (now < sunrise)
        {
            var timeUntilSunrise = sunrise.Value - now;
            sb.AddLine("Weather:Generic:Sunrise", sunrise.Value.ToOffset(UtcOffset), timeUntilSunrise.Format(lang));
        }
        else if (now < sunset)
        {
            var timeUntilSunset = sunset.Value - now;
            sb.AddLine("Weather:Generic:Sunset", sunset.Value.ToOffset(UtcOffset), timeUntilSunset.Format(lang));
        }
        else if (sunset != null)
        {
            sb.AddLine("Weather:Generic:SunsetAlready", sunset.Value.ToOffset(UtcOffset));
        }
    }

    private static void AppendSingleDetails(TranslatedBuilder sb, GenericWeatherItem w)
    {
        sb.Add("Weather:Single:WeatherName");

        if (w.WeatherIcon != null)
        {
            sb.AddRaw($"{w.WeatherIcon} ");
        }

        if (w.WeatherName.StartsWith('!'))
        {
            sb.AddLine(w.WeatherName[1..]);
        }
        else
        {
            sb.AddRawLine(w.WeatherName);
        }

        sb.AddLine("Weather:Single:Temperature", (int)Math.Round(w.Temperature.MinBy(x => x.Key).Value));
        if (w.Humidity != null)
        {
            sb.AddLine("Weather:Generic:Humidity", w.Humidity);
        }

        sb.AddLine();

        if (w.Visibility != null)
        {
            sb.AddLine("Weather:Generic:Visibility", w.Visibility / 1000);
        }

        var wind = w.WindSpeed.MinBy(x => x.Key).Value;
        sb.AddLine("Weather:Single:WindSpeed", wind, WindLevel.Get(wind));

        var gusts = w.WindGusts.MinBy(x => x.Key).Value;
        sb.AddLine("Weather:Single:WindGust", gusts, WindLevel.Get(gusts));

        sb.AddLine();
    }
}

public record WeatherReportFormatResult(string Message, int Page, int PageCount);