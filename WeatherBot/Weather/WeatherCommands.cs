﻿using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WeatherBot.Bookmarks.Database;
using WeatherBot.Text;
using WeatherBot.Users;
using WeatherBot.Weather.Models;
using Emoji = WeatherBot.Text.Emoji;

namespace WeatherBot.Weather;

public static class WeatherCommands
{
    #region Chat messages

    public static Task LocationMessage(BotUser user, Message message)
    {
        return HandleWeather(
            user,
            message.Chat,
            new WeatherParams(user.WeatherType, message.Location!.Latitude, message.Location!.Longitude),
            reply: new ReplyParameters { MessageId = message.MessageId }
        );
    }

    public static async Task BookmarkMessage(BotUser user, Message message)
    {
        var bookmark = BookmarkEntity.TryGet(user.Id, message.Text![1..])
                       ?? throw new UserException("Bookmark not found");

        await HandleWeather(
            user,
            message.Chat,
            new WeatherParams(bookmark.Type, bookmark.Lat, bookmark.Lon),
            bookmark: bookmark
        );
    }

    #endregion

    #region Inline keyboard callbacks

    public static async Task WeatherCallback(BotUser user, CallbackQuery query, string[] args)
    {
        if (!WeatherParams.TryParse(args, 1, out var request))
            throw new Exception($"Unexpected data for Weather: {query.Data}");

        var bookmark = BookmarkEntity.TryGet(user.Id, request);

        await HandleWeather(user, query.Message!.Chat, request, bookmark: bookmark);
        await App.Bot.AnswerCallbackQuery(query.Id, text: null);
    }

    public static async Task SetTypeCallback(BotUser user, CallbackQuery query, string[] args)
    {
        if (!WeatherParams.TryParse(args, 1, out var request))
            throw new Exception("Weather parameters not found");

        await App.Bot.AnswerCallbackQuery(query.Id);

        await App.Bot.SendMessage(
            query.Message!.Chat,
            Translator.Get(user.Language, "Weather:Misc:SetTypePrompt"),
            replyMarkup: new InlineKeyboardMarkup(
                WeatherReportTypeExtensions.ButtonOrder
                    .Select(row => row.Select(key => new InlineKeyboardButton
                    {
                        Text = Translator.Get(user.Language, $"FetchType:{key}:ShortName"),
                        CallbackData = $"Weather {request.With(WeatherReportTypeExtensions.All[key].Type)}"
                    }))
            )
        );
    }

    public static async Task WeatherPageCallback(BotUser user, CallbackQuery query, string[] args)
    {
        if (!int.TryParse(args.ElementAtOrDefault(1), out var cacheId) ||
            !int.TryParse(args.ElementAtOrDefault(2), out var page))
        {
            throw new Exception($"Unexpected data for WeatherPage: {query.Data}");
        }

        var cached = WeatherLog.TryGet(cacheId) ?? throw new UserException("!Weather:Misc:CacheExpired");

        var definition = cached.Request.Type.GetDefinition();
        var formatted = definition.Format(cached.Response, user.Language, page, query.Message!.Date);

        var bookmark = BookmarkEntity.TryGet(user.Id, cached.Request);

        await App.Bot.EditMessageText(
            query.Message.Chat,
            query.Message.MessageId,
            text: formatted.Message,
            parseMode: ParseMode.Html,
            replyMarkup: GetWeatherMarkup(cached, formatted, bookmark?.Id)
        );
    }

    public static async Task WeatherInfoCallback(BotUser user, CallbackQuery query, string[] args)
    {
        if (!int.TryParse(args.ElementAtOrDefault(1), out var cacheId))
        {
            throw new Exception($"Unexpected data for WeatherInfo: {query.Data}");
        }

        var cached = WeatherLog.TryGet(cacheId) ?? throw new UserException("!Weather:Misc:CacheExpired");

        var sb = new StringBuilder();
        sb.AppendLine(user.Translate($"FetchType:{cached.Request.Type.GetKey()}:FullName"));
        sb.Append(cached.DateTimeUtc.Add(cached.Response.UtcOffset).ToString(user.Culture));
        sb.AppendLine($" UTC+{cached.Response.UtcOffset.Hours:00}:{cached.Response.UtcOffset.Minutes:00}");
        sb.AppendLine();
        sb.AppendLine($"[id:{cached.Id} lat:{cached.Request.Lat} lon:{cached.Request.Lon}]");

        await App.Bot.AnswerCallbackQuery(query.Id, sb.ToString(), showAlert: true);
    }

    #endregion

    #region Private

    private static async Task HandleWeather(BotUser user, Chat chat, WeatherParams request,
        ReplyParameters? reply = null, BookmarkEntity? bookmark = null)
    {
        var def = request.Type.GetDefinition();
        GenericWeatherResponse? response;

        var cached = WeatherLog.TryGet(request);
        if (cached != null)
        {
            response = cached.Response;
        }
        else
        {
            var requestsInHour = WeatherLog.Count(user.Id, TimeSpan.FromHours(1));
            var requestsQuota = user.RequestsQuota ?? App.Config.Users.DefaultRequestsQuota;
            if (requestsInHour >= requestsQuota)
            {
                await App.Bot.SendMessage(chat, Translator.Format(user.Language, "Quota:Reached", requestsQuota));
                return;
            }

            response = await def.Fetch(request.Lat, request.Lon, user.Language);
            cached = WeatherLog.Create(user.Id, request, response);
        }

        var formatted = def.Format(response, user.Language, page: 0, DateTime.UtcNow);

        await App.Bot.SendMessage(
            chatId: chat,
            text: formatted.Message,
            replyParameters: reply,
            parseMode: ParseMode.Html,
            replyMarkup: GetWeatherMarkup(cached, formatted, bookmark?.Id)
        );
    }

    private static InlineKeyboardMarkup GetWeatherMarkup(WeatherLog weather, WeatherReportFormatResult formatted,
        int? bookmarkId)
    {
        var markup = new InlineKeyboardMarkup();

        if (formatted.PageCount > 1)
        {
            markup.AddButton($"{formatted.Page + 1} / {formatted.PageCount}", $"WeatherPage {weather.Id} {formatted.Page + 1}");
        }

        markup.AddButton(Emoji.Refresh, $"Weather {weather.Request}");

        markup.AddButton(Emoji.TwistedArrows, $"WeatherSetType {weather.Request}");

        markup.AddButton(bookmarkId == null
            ? new InlineKeyboardButton(Emoji.Star) { CallbackData = $"AddBookmark {weather.Request}" }
            : new InlineKeyboardButton(Emoji.TrashBin) { CallbackData = $"DeleteBookmark {bookmarkId}" }
        );

        markup.AddButton(Emoji.Info, $"WeatherInfo {weather.Id}");

        return markup;
    }

    #endregion
}