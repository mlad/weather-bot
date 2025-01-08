# Weather Bot

Weather bot for Telegram Messenger with multiple APIs and bookmarks support.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/36ac4903-dfae-4b97-bf05-755a0e5cbc3f">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/8ebfd0ec-c07b-42d7-b5d4-3ff7bd664e27">
  <img alt="Bot UI preview" src="https://github.com/user-attachments/assets/8ebfd0ec-c07b-42d7-b5d4-3ff7bd664e27">
</picture>

## Features

- Multiple APIs support:
    - [Open Weather Map](https://openweathermap.org/api)
    - [Open-Meteo](https://open-meteo.com/)
    - [AccuWeather](https://developer.accuweather.com/)
- Different report types:
    - [Daily forecast](https://github.com/user-attachments/assets/6065d2a1-4a4e-46b9-883b-ab9410d01295)
    - [Current weather and hourly forecast](https://github.com/user-attachments/assets/6ef42bc1-6e88-4529-9eb2-b2dcf7d680a9)
    - [Hourly forecast on different heights](https://github.com/user-attachments/assets/4880be10-31b3-4d5f-8e24-29585aaf6d41) (10m, 80m, 120m and 180m)
    - [Hourly forecast summary table](https://github.com/user-attachments/assets/b47a0927-8475-4a17-966d-2df26a64da02) (using image)
- Weather by location (using built-in Telegram location sharing)
- Text location search (using [Open Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api))
- Beaufort Scale wind level display
- Per-user bookmarks
- Per-user quotas on API requests
- Multiple languages support. Currently translated to:
    - English
    - Russian
- API responses caching

## Usage

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for your platform
2. Download the [latest release](https://github.com/mlad/weather-bot/releases) or [build it yourself](#building), unpack the archive
3. Run `WeatherBot` (Linux) or `WeatherBot.exe` (Windows), the app will create `configuration.json` file and exit
4. Open created configuration file and fill the following properties:
    - `TelegramBotToken` with a Telegram bot API token (can be obtained from [@BotFather](https://t.me/BotFather) bot)
    - `OpenWeatherMap:ApiToken` with Open Weather Map API token (register [here](https://home.openweathermap.org/users/sign_up)), or completely remove `OpenWeatherMap` section
    - `AccuWeather:ApiToken` with AccuWeather API token (register [here](https://developer.accuweather.com/user/register)), or completely remove `AccuWeather` section
5. Run the application again. If authorization is successful, console will display "Logged-in as {bot name}"
6. Send `/start` or `/help` to the bot to get list of commands. Share a location to get weather

## Building

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for your platform
2. In project directory, run `dotnet publish ./WeatherBot/WeatherBot.csproj`
3. Binaries will be located in `./WeatherBot/bin/Release/net8.0/publish`

## Libraries used

- [Telegram.Bot](https://github.com/TelegramBots/telegram.bot) (MIT license)
- [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) (MIT license)
- [CoordinateSharp](https://github.com/Tronald/CoordinateSharp) (AGPL-3.0 license)
- [Magick.NET-Q8-AnyCPU](https://github.com/dlemstra/Magick.NET) (Apache-2.0 license)
