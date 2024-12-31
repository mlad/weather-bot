using System.Text;
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

        var definition = (BasicReportDefinition)cached.Request.Type.GetDefinition();
        var formatted = definition.Format(cached.Response, user.Language, page, query.Message!.Date);

        var bookmark = BookmarkEntity.TryGet(user.Id, cached.Request);

        await App.Bot.EditMessageText(
            query.Message.Chat,
            query.Message.MessageId,
            text: formatted.Message,
            parseMode: ParseMode.Html,
            replyMarkup: GetWeatherMarkup(cached.Request, formatted.Page, formatted.PageCount, bookmark?.Id, cached.Id)
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

    private static Task HandleWeather(
        BotUser user,
        Chat chat,
        WeatherParams request,
        ReplyParameters? reply = null,
        BookmarkEntity? bookmark = null)
    {
        return request.Type.GetDefinition() switch
        {
            BasicReportDefinition def => HandleBasic(def),
            InheritedReportDefinition def => HandleInherited(def),
            _ => throw new Exception($"Unexpected report definition type ({request.Type.GetDefinition().GetType()})")
        };

        async Task HandleBasic(BasicReportDefinition def)
        {
            var weather = await GetWeather(user, request);

            var formatted = def.Format(weather.Response, user.Language, page: 0, DateTime.UtcNow);

            await App.Bot.SendMessage(
                chatId: chat,
                text: formatted.Message,
                replyParameters: reply,
                parseMode: ParseMode.Html,
                replyMarkup: GetWeatherMarkup(request, formatted.Page, formatted.PageCount, bookmark?.Id, weather.Id)
            );
        }

        async Task HandleInherited(InheritedReportDefinition def)
        {
            var weather = await Task.WhenAll(
                def.BaseTypes.Where(x => x.GetDefinition().IsAvailable()).Select(x => GetWeather(user, request.With(x)))
            );

            var image = def.Format(user, weather);

            await App.Bot.SendPhoto(
                chatId: chat,
                photo: InputFile.FromStream(new MemoryStream(image)),
                replyParameters: reply,
                parseMode: ParseMode.Html,
                replyMarkup: GetWeatherMarkup(request, page: 0, pageCount: 1, bookmark?.Id, cacheId: null)
            );
        }
    }

    private static async Task<WeatherLog> GetWeather(BotUser user, WeatherParams request)
    {
        var cached = WeatherLog.TryGet(request);
        if (cached == null)
        {
            var def = (BasicReportDefinition)request.Type.GetDefinition();
            var response = await def.Fetch(request.Lat, request.Lon, user.Language);
            cached = WeatherLog.Create(user.Id, request, response);
        }

        return cached;
    }

    private static InlineKeyboardMarkup GetWeatherMarkup(
        WeatherParams request,
        int page,
        int pageCount,
        int? bookmarkId,
        int? cacheId)
    {
        var markup = new InlineKeyboardMarkup();

        if (pageCount > 1 && cacheId != null)
        {
            markup.AddButton($"{page + 1} / {pageCount}", $"WeatherPage {cacheId} {page + 1}");
        }

        markup.AddButton(Emoji.Refresh, $"Weather {request}");

        markup.AddButton(Emoji.TwistedArrows, $"WeatherSetType {request}");

        markup.AddButton(bookmarkId == null
            ? new InlineKeyboardButton(Emoji.Star) { CallbackData = $"AddBookmark {request}" }
            : new InlineKeyboardButton(Emoji.TrashBin) { CallbackData = $"DeleteBookmark {bookmarkId}" }
        );

        if (cacheId != null)
        {
            markup.AddButton(Emoji.Info, $"WeatherInfo {cacheId}");
        }

        return markup;
    }

    #endregion
}