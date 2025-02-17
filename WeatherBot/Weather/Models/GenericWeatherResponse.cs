﻿using System.Globalization;
using CoordinateSharp;
using WeatherBot.Text;

namespace WeatherBot.Weather.Models;

public class GenericWeatherResponse
{
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required IReadOnlyList<GenericWeatherItem> Items { get; init; }
    public required TimeSpan UtcOffset { get; init; }

    public WeatherReportFormatResult FormatHourlyMultiHeight(string lang, int page, DateTime now)
    {
        var sb = new TranslatedBuilder(lang);

        var startDate = now.Hour();
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

            AppendWeatherName(sb, item);

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

    public WeatherReportFormatResult FormatHourly(string lang, int page, DateTime utcNow)
    {
        var sb = new TranslatedBuilder(lang);

        var now = new DateTimeOffset(utcNow).ToOffset(UtcOffset);
        var nowHour = now.Hour();

        var grouped = Items
            .Where(x => x.Time >= nowHour)
            .Select(x => new { LocalTime = x.Time.ToOffset(UtcOffset), Weather = x })
            .GroupBy(x => x.LocalTime.Date)
            .ToList();

        var offset = grouped[0].First().LocalTime.Hour() == nowHour ? 1 : 0; // 1 if contains current weather, 0 otherwise

        var pageCount = (int)Math.Ceiling((grouped.Count - offset) / (double)App.Config.Weather.HourlyDaysPerPage) + offset;
        if (page < 0 || page >= pageCount)
            page = 0;

        if (page == 0 && offset == 1)
        {
            var idx = 0;
            foreach (var w in grouped[0])
            {
                switch (idx++)
                {
                    case 0:
                        sb.AddLine("Weather:Hourly:Now");
                        AppendSingleDetails(sb, w.Weather);
                        AppendTimeDetails(sb, lang, now);
                        break;
                    case 1:
                        sb.AddLine();
                        sb.AddLine("Weather:Hourly:Today");
                        AppendHour(w.Weather, w.LocalTime);
                        break;
                    default:
                        AppendHour(w.Weather, w.LocalTime);
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
                var nowDate = now.Date;

                if (day.Key == nowDate)
                {
                    sb.AddLine("Weather:Hourly:Today");
                }
                else
                {
                    sb.AddLine(
                        "Weather:Hourly:Day",
                        Translator.Get(lang, $"Weekday:{day.Key.ToString("dddd", CultureInfo.InvariantCulture)}"),
                        day.Key.Day,
                        Translator.Get(lang, $"Month:{day.Key.ToString("MMMM", CultureInfo.InvariantCulture)}")
                    );
                }

                var nth = Math.Max(1, (int)Math.Ceiling(day.Count() / (double)App.Config.Weather.HourlyItemsPerDay));

                foreach (var w in day)
                {
                    if (w.LocalTime.Hour % nth != 0)
                        continue;

                    AppendHour(w.Weather, w.LocalTime);
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
                w.GetIconEmoji(),
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
                w.GetIconEmoji(),
                (int)Math.Round(w.Temperature.MinBy(x => x.Key).Value),
                wind,
                WindLevel.Get(wind)
            );
        }

        return new WeatherReportFormatResult(sb.ToString(), page, pageCount);
    }

    private void AppendTimeDetails(TranslatedBuilder sb, string lang, DateTimeOffset now)
    {
        var localTime = now.ToOffset(UtcOffset).DateTime;
        sb.AddLine("Weather:Generic:LocalTime", localTime);

        var celestial = new Celestial(Latitude, Longitude, localTime, UtcOffset.TotalHours);
        var sunrise = celestial.SunRise;
        var sunset = celestial.SunSet;

        if (localTime < sunrise)
        {
            var timeUntilSunrise = sunrise.Value - localTime;
            sb.AddLine("Weather:Generic:Sunrise", sunrise.Value, timeUntilSunrise.Format(lang));
        }
        else if (localTime < sunset)
        {
            var timeUntilSunset = sunset.Value - localTime;
            sb.AddLine("Weather:Generic:Sunset", sunset.Value, timeUntilSunset.Format(lang));
        }
        else if (sunset != null)
        {
            sb.AddLine("Weather:Generic:SunsetAlready", sunset.Value);
        }
    }

    private static void AppendSingleDetails(TranslatedBuilder sb, GenericWeatherItem w)
    {
        sb.Add("Weather:Single:WeatherName");

        AppendWeatherName(sb, w);

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

    private static void AppendWeatherName(TranslatedBuilder sb, GenericWeatherItem w)
    {
        var emoji = w.GetIconEmoji();
        if (emoji != null)
        {
            sb.AddRaw($"{emoji} ");
        }

        if (w.WeatherName.StartsWith('!'))
        {
            sb.AddLine(w.WeatherName[1..]);
        }
        else
        {
            sb.AddRawLine(w.WeatherName);
        }
    }
}

public record WeatherReportFormatResult(string Message, int Page, int PageCount);