# Weather Bot

Weather bot for Telegram Messenger with multiple APIs and bookmarks support.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://github.com/user-attachments/assets/36ac4903-dfae-4b97-bf05-755a0e5cbc3f">
  <source media="(prefers-color-scheme: light)" srcset="https://github.com/user-attachments/assets/8ebfd0ec-c07b-42d7-b5d4-3ff7bd664e27">
  <img alt="Bot UI preview" src="https://github.com/user-attachments/assets/8ebfd0ec-c07b-42d7-b5d4-3ff7bd664e27">
</picture>

## Features

- Multiple APIs support:
    - [Open Weather Map](https://openweathermap.org/api) (free, token required)
    - [Open Meteo](https://open-meteo.com/) (free, no token required)
- Different report types:
    - Open Weather Map
        - Current weather
        - 3-hour forecast 5 days
    - Open Meteo
        - Current weather
        - 7 days forecast
        - 3-hour forecast 7 days
        - Hourly forecast on different heights (10m, 80m, 120m and 180m)
- Weather by location (using built-in Telegram location sharing)
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
