using SQLite;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WeatherBot.Bookmarks;
using WeatherBot.Bookmarks.Database;
using WeatherBot.Bookmarks.Models;
using WeatherBot.Text;
using WeatherBot.Users;
using WeatherBot.Users.Database;
using WeatherBot.Weather;
using WeatherBot.Weather.Database;
using WeatherBot.Weather.Models;

namespace WeatherBot;

internal class App
{
    public static SQLiteConnection Database { get; private set; } = default!;
    public static AppConfiguration Config { get; private set; } = default!;
    public static TelegramBotClient Bot { get; private set; } = default!;

    public static readonly string[] AvailableCommands = ["/lang", "/help"];

    private static void Main()
    {
        new App().MainAsync().GetAwaiter().GetResult();
    }

    private async Task MainAsync()
    {
        Config = AppConfiguration.ReadOrCreate();
        Config.Validate();

        Database = new SQLiteConnection("database.db", storeDateTimeAsTicks: false);
        Database.CreateTable<BotUserEntity>();
        Database.CreateTable<WeatherLogEntity>();
        Database.CreateTable<BookmarkEntity>();

        Bot = new TelegramBotClient(Config.TelegramBotToken);
        var me = await Bot.GetMe();
        Console.WriteLine($"Logged-in as {me.FirstName} ({me.Id})");
        Bot.OnMessage += OnMessageHandler;
        Bot.OnUpdate += OnUpdateHandler;

        foreach (var lang in Translator.AllLanguages)
        {
            await Bot.SetMyCommands(GetBotCommands(lang), languageCode: lang);
        }

        var isCancelled = false;
        Console.CancelKeyPress += (_, _) => isCancelled = true;
        while (!isCancelled)
        {
            await Task.Delay(1000);
        }

        Database.Dispose();
    }

    private async Task OnMessage(Message message, BotUser user)
    {
        var args = message.Text?.Split(' ') ?? [];

        switch (args.ElementAtOrDefault(0))
        {
            case "/start":
            case "/help":
                await BotUserCommands.Help(user, message);
                break;
            case "/lang":
                await BotUserCommands.Lang(user, message, args);
                break;
            case "/setquota" when Config.AdminsIds.Contains(user.Id):
                await BotUserCommands.SetQuota(message, args);
                break;
            case null when message.Location is not null:
                await WeatherCommands.LocationMessage(user, message);
                break;
            default:
            {
                if (string.IsNullOrEmpty(message.Text)) break;

                if (message.Text.StartsWith('/'))
                {
                    if (WeatherReportTypeExtensions.TryParse(message.Text[1..], out var type))
                    {
                        await BotUserCommands.SetWeatherType(user, message, type);
                    }
                }
                else if (message.Text.StartsWith(BookmarkConfiguration.Prefix))
                {
                    await WeatherCommands.BookmarkMessage(user, message);
                }
                else
                {
                    await BookmarkCommands.SetName(user, message);
                }

                break;
            }
        }
    }

    private async Task OnButtonPressed(CallbackQuery query, BotUser user)
    {
        var args = query.Data?.Split(' ') ?? [];

        switch (args.ElementAtOrDefault(0))
        {
            case "Weather":
                await WeatherCommands.WeatherCallback(user, query, args);
                break;
            case "WeatherSetType":
                await WeatherCommands.SetTypeCallback(user, query, args);
                break;
            case "WeatherPage":
                await WeatherCommands.WeatherPageCallback(user, query, args);
                break;
            case "AddBookmark":
                await BookmarkCommands.Add(user, query, args);
                break;
            case "DeleteBookmark":
                await BookmarkCommands.Delete(user, query, args);
                break;
        }
    }

    public static IReplyMarkup GetMainMenuMarkup(long userId)
    {
        var bookmarks = BookmarkEntity.List(userId);

        return bookmarks.Count != 0
            ? new ReplyKeyboardMarkup
            {
                Keyboard = bookmarks
                    .Chunk(Config.Bookmarks.BookmarksPerRow)
                    .Select(row => row.Select(x => new KeyboardButton(BookmarkConfiguration.Prefix + x.Name))),
                ResizeKeyboard = true,
                IsPersistent = true
            }
            : new ReplyKeyboardRemove();
    }

    private async Task OnMessageHandler(Message message, UpdateType type)
    {
        if (message.From == null) return;
        var user = BotUser.GetOrCreate(message.From);

        if (user.Name != message.From.Username)
        {
            user.Name = message.From.Username;
            user.Update();
        }

        try
        {
            await OnMessage(message, user);
        }
        catch (UserException ex)
        {
            await Bot.SendMessage(
                message.Chat,
                ex.Message.StartsWith('!') ? Translator.Get(user.Language, ex.Message[1..]) : ex.Message
            );
        }
        catch (Exception ex)
        {
            if (Config.AdminsIds.Contains(user.Id))
            {
                Console.WriteLine(ex.ToString());
                await Bot.SendMessage(message.Chat, $"Exception: {ex.Message}");
            }
            else
            {
                await Bot.SendMessage(message.Chat, "Failed to handle command. Please try again later");
            }
        }
    }

    private async Task OnUpdateHandler(Update update)
    {
        if (update is { CallbackQuery: { } query })
        {
            var user = BotUser.GetOrCreate(query.From);

            try
            {
                await OnButtonPressed(query, user);
            }
            catch (UserException ex)
            {
                await Bot.AnswerCallbackQuery(
                    query.Id,
                    text: ex.Message.StartsWith('!') ? Translator.Get(user.Language, ex.Message[1..]) : ex.Message
                );
            }
            catch (Exception ex)
            {
                await Bot.AnswerCallbackQuery(query.Id);

                if (Config.AdminsIds.Contains(user.Id))
                {
                    Console.WriteLine(ex.ToString());
                    await Bot.SendMessage(query.Message!.Chat, $"Exception: {ex.Message}");
                }
                else
                {
                    await Bot.SendMessage(query.Message!.Chat, "Failed to handle command. Please try again later");
                }
            }
        }
    }

    private static IEnumerable<BotCommand> GetBotCommands(string lang)
    {
        return WeatherReportTypeExtensions.All.Keys
            .Select(key => new BotCommand
            {
                Command = $"/{key}",
                Description = Translator.Get(lang, $"FetchType:{key}:ShortName")
            })
            .Concat(AvailableCommands.Select(command => new BotCommand
            {
                Command = command,
                Description = Translator.Get(lang, $"Help:{command}")
            }));
    }
}