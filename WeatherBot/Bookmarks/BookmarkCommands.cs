using Telegram.Bot;
using Telegram.Bot.Types;
using WeatherBot.Bookmarks.Database;
using WeatherBot.Text;
using WeatherBot.Users;
using WeatherBot.Weather.Models;

namespace WeatherBot.Bookmarks;

public static class BookmarkCommands
{
    public static async Task Add(BotUser user, CallbackQuery query, string[] args)
    {
        if (!WeatherParams.TryParse(args, 1, out var request))
            throw new Exception($"Unexpected data for AddBookmark: {query.Data}");

        if (BookmarkEntity.Count(user.Id) >= App.Config.Bookmarks.MaxUserBookmarks)
            throw new UserException(Translator.Format(user.Language, "Bookmark:LimitReached", App.Config.Bookmarks.MaxUserBookmarks));

        if (BookmarkEntity.TryGet(user.Id, request) != null)
            throw new UserException("!Bookmark:PointAlreadyExists");

        var existing = BookmarkEntity.TryGet(user.Id, name: null);
        if (existing != null)
        {
            BookmarkEntity.Delete(user.Id, existing.Id);
        }

        BookmarkEntity.Create(user.Id, request);

        await App.Bot.SendMessage(query.Message!.Chat, Translator.Get(user.Language, "Bookmark:AskName"));
        await App.Bot.AnswerCallbackQuery(query.Id, text: null);
    }

    public static async Task Delete(BotUser user, CallbackQuery query, string[] args)
    {
        if (!int.TryParse(args.ElementAtOrDefault(1), out var id))
            throw new Exception($"Unexpected data for DeleteBookmark: {query.Data}");

        if (!BookmarkEntity.Delete(user.Id, id))
            throw new UserException("!Bookmark:AlreadyDeleted");

        await App.Bot.SendMessage(
            query.Message!.Chat,
            Translator.Get(user.Language, "Bookmark:Deleted"),
            replyMarkup: App.GetMainMenuMarkup(user.Id)
        );

        await App.Bot.AnswerCallbackQuery(query.Id, text: null);
    }

    public static async Task SetName(BotUser user, Message message)
    {
        if (string.IsNullOrEmpty(message.Text)) return;

        var bookmark = BookmarkEntity.TryGet(user.Id, name: null);
        if (bookmark is null) return;

        bookmark.UpdateName(message.Text.Trim());

        await App.Bot.SendMessage(
            message.Chat,
            Translator.Get(user.Language, "Bookmark:Created"),
            replyMarkup: App.GetMainMenuMarkup(user.Id)
        );
    }
}