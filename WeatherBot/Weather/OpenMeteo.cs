﻿using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherBot.Text;
using WeatherBot.Weather.Models;

namespace WeatherBot.Weather;

public static class OpenMeteo
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private const string Endpoint = "https://api.open-meteo.com/v1/forecast";

    public static async Task<GenericWeatherResponse> GetCurrent(double lat, double lon)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"{Endpoint}?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,weather_code," +
            $"wind_speed_10m,wind_gusts_10m&hourly=visibility&wind_speed_unit=ms&timeformat=unixtime&timezone=auto" +
            $"&forecast_days=1"
        );
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<CurrentResponse>(stream, JsonOptions)!.ToGeneric();
    }

    public static async Task<GenericWeatherResponse> GetDaily(double lat, double lon)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"{Endpoint}?latitude={lat}&longitude={lon}&daily=weather_code,temperature_2m_max,temperature_2m_min," +
            $"wind_speed_10m_max,wind_gusts_10m_max&wind_speed_unit=ms&timeformat=unixtime&timezone=auto&forecast_days=14"
        );
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<DailyResponse>(stream, JsonOptions)!.ToGeneric();
    }

    public static async Task<GenericWeatherResponse> GetHourly(double lat, double lon)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"{Endpoint}?latitude={lat}&longitude={lon}&hourly=temperature_2m,relative_humidity_2m," +
            $"weather_code,visibility,wind_speed_10m,wind_gusts_10m&wind_speed_unit=ms&timeformat=unixtime&" +
            $"timezone=auto&forecast_days=6"
        );
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<HourlyResponse>(stream, JsonOptions)!.ToGeneric();
    }

    public static async Task<GenericWeatherResponse> GetHourlyMultiHeight(double lat, double lon)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"{Endpoint}?latitude={lat}&longitude={lon}&hourly=temperature_2m,relative_humidity_2m,weather_code," +
            $"visibility,wind_speed_10m,wind_speed_80m,wind_speed_120m,wind_speed_180m,wind_gusts_10m,temperature_80m," +
            $"temperature_120m,temperature_180m&wind_speed_unit=ms&timeformat=unixtime&timezone=auto&forecast_days=2"
        );
        await using var stream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<HourlyResponse>(stream, JsonOptions)!.ToGeneric();
    }

    [Serializable]
    public class HourlyResponse
    {
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required int UtcOffsetSeconds { get; set; }
        public required HourlyModel Hourly { get; set; }

        public GenericWeatherResponse ToGeneric() => new()
        {
            Latitude = Latitude,
            Longitude = Longitude,
            Items = Hourly.Time.Select((t, i) =>
            {
                var item = new GenericWeatherItem
                {
                    Time = DateTimeOffset.FromUnixTimeSeconds(t),
                    WeatherName = GetWeatherName(Hourly.WeatherCode[i]),
                    WeatherIcon = GetWeatherEmoji(Hourly.WeatherCode[i]),
                    Humidity = Hourly.Humidity[i],
                    Visibility = Hourly.Visibility[i],
                    Temperature = new Dictionary<int, double> { [10] = Hourly.Temperature2M[i] }, // using 10m for consistency
                    WindSpeed = new Dictionary<int, double> { [10] = Hourly.WindSpeed10M[i] },
                    WindGusts = new Dictionary<int, double> { [10] = Hourly.WindGusts10M[i] }
                };

                if (Hourly.Temperature80M != null) item.Temperature[80] = Hourly.Temperature80M[i];
                if (Hourly.Temperature120M != null) item.Temperature[120] = Hourly.Temperature120M[i];
                if (Hourly.Temperature180M != null) item.Temperature[180] = Hourly.Temperature180M[i];

                if (Hourly.WindSpeed80M != null) item.WindSpeed[80] = Hourly.WindSpeed80M[i];
                if (Hourly.WindSpeed120M != null) item.WindSpeed[120] = Hourly.WindSpeed120M[i];
                if (Hourly.WindSpeed180M != null) item.WindSpeed[180] = Hourly.WindSpeed180M[i];

                return item;
            }).ToList(),
            UtcOffset = TimeSpan.FromSeconds(UtcOffsetSeconds)
        };

        [Serializable]
        public class HourlyModel
        {
            public required int[] Time { get; set; }
            public required int[] WeatherCode { get; set; }
            public required double[] Visibility { get; set; }

            [JsonPropertyName("relative_humidity_2m")]
            public required int[] Humidity { get; set; }

            [JsonPropertyName("temperature_2m")] public required double[] Temperature2M { get; set; }
            [JsonPropertyName("temperature_80m")] public double[]? Temperature80M { get; set; }
            [JsonPropertyName("temperature_120m")] public double[]? Temperature120M { get; set; }
            [JsonPropertyName("temperature_180m")] public double[]? Temperature180M { get; set; }

            [JsonPropertyName("wind_speed_10m")] public required double[] WindSpeed10M { get; set; }
            [JsonPropertyName("wind_speed_80m")] public double[]? WindSpeed80M { get; set; }
            [JsonPropertyName("wind_speed_120m")] public double[]? WindSpeed120M { get; set; }
            [JsonPropertyName("wind_speed_180m")] public double[]? WindSpeed180M { get; set; }

            [JsonPropertyName("wind_gusts_10m")] public required double[] WindGusts10M { get; set; }
        }
    }

    [Serializable]
    public class DailyResponse
    {
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required int UtcOffsetSeconds { get; set; }
        public required DailyModel Daily { get; set; }

        public GenericWeatherResponse ToGeneric() => new()
        {
            Latitude = Latitude,
            Longitude = Longitude,
            Items = Daily.Time.Select((t, i) => new GenericWeatherItem
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(t),
                WeatherName = GetWeatherName(Daily.WeatherCode[i]),
                WeatherIcon = GetWeatherEmoji(Daily.WeatherCode[i]),
                Temperature = new Dictionary<int, double> { [2] = (Daily.TemperatureMin[i] + Daily.TemperatureMax[i]) / 2 },
                WindSpeed = new Dictionary<int, double> { [10] = Daily.WindSpeed[i] },
                WindGusts = new Dictionary<int, double> { [10] = Daily.WindGusts[i] }
            }).ToList(),
            UtcOffset = TimeSpan.FromSeconds(UtcOffsetSeconds)
        };

        [Serializable]
        public class DailyModel
        {
            public required int[] Time { get; set; }
            public required int[] WeatherCode { get; set; }

            [JsonPropertyName("temperature_2m_max")]
            public required double[] TemperatureMax { get; set; }

            [JsonPropertyName("temperature_2m_min")]
            public required double[] TemperatureMin { get; set; }

            [JsonPropertyName("wind_speed_10m_max")]
            public required double[] WindSpeed { get; set; }

            [JsonPropertyName("wind_gusts_10m_max")]
            public required double[] WindGusts { get; set; }
        }
    }

    [Serializable]
    public class CurrentResponse
    {
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required int UtcOffsetSeconds { get; set; }
        public required CurrentModel Current { get; set; }
        public required HourlyModel Hourly { get; set; }

        public GenericWeatherResponse ToGeneric()
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(Current.Time);
            var hour = new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

            return new GenericWeatherResponse
            {
                Latitude = Latitude,
                Longitude = Longitude,
                Items =
                [
                    new GenericWeatherItem
                    {
                        Time = time,
                        WeatherName = GetWeatherName(Current.WeatherCode),
                        WeatherIcon = GetWeatherEmoji(Current.WeatherCode),
                        Humidity = Current.Humidity,
                        Visibility = Hourly.Visibility[Array.FindIndex(Hourly.Time, x => x == hour)],
                        Temperature = new Dictionary<int, double> { [2] = Current.Temperature },
                        WindSpeed = new Dictionary<int, double> { [10] = Current.WindSpeed },
                        WindGusts = new Dictionary<int, double> { [10] = Current.WindGusts }
                    }
                ],
                UtcOffset = TimeSpan.FromSeconds(UtcOffsetSeconds)
            };
        }

        [Serializable]
        public class CurrentModel
        {
            public required int Time { get; set; }
            public required int WeatherCode { get; set; }
            [JsonPropertyName("temperature_2m")] public required double Temperature { get; set; }
            [JsonPropertyName("wind_speed_10m")] public required double WindSpeed { get; set; }
            [JsonPropertyName("wind_gusts_10m")] public required double WindGusts { get; set; }

            [JsonPropertyName("relative_humidity_2m")]
            public required int Humidity { get; set; }
        }

        [Serializable]
        public class HourlyModel
        {
            public required int[] Time { get; set; }
            public required double[] Visibility { get; set; }
        }
    }

    private static string GetWeatherName(int code) => code switch
    {
        0 => "!Weather:Name:ClearSky",
        1 or 2 or 3 => "!Weather:Name:PartlyCloudy",
        45 or 48 => "!Weather:Name:Fog",
        51 or 53 or 55 => "!Weather:Name:Drizzle",
        56 or 57 => "!Weather:Name:FreezingDrizzle",
        61 or 63 or 65 => "!Weather:Name:Rain",
        66 or 67 => "!Weather:Name:FreezingRain",
        71 or 73 or 75 => "!Weather:Name:Snowfall",
        77 => "!Weather:Name:SnowGrains",
        80 or 81 or 82 => "!Weather:Name:RainShowers",
        85 or 86 => "!Weather:Name:SnowShowers",
        95 => "!Weather:Name:Thunderstorm",
        96 or 99 => "!Weather:Name:ThunderstormWithHail",
        _ => "unknown"
    };

    private static string GetWeatherEmoji(int code) => code switch
    {
        0 => Emoji.ClearSky,
        1 or 2 or 3 => Emoji.FewClouds,
        45 or 48 => Emoji.Fog,
        51 or 53 or 55 => Emoji.Drizzle,
        56 or 57 => Emoji.Snow + Emoji.Drizzle,
        61 or 63 or 65 => Emoji.Rain,
        66 or 67 => Emoji.Snow + Emoji.Rain,
        71 or 73 or 75 => Emoji.Snow + Emoji.Snow + Emoji.Snow,
        77 => Emoji.Snow,
        80 or 81 or 82 => Emoji.Rain + Emoji.Rain,
        85 or 86 => Emoji.Snow + Emoji.Snow,
        95 => Emoji.Thunderstorm,
        96 or 99 => Emoji.Thunderstorm + Emoji.Rock,
        _ => string.Empty
    };
}