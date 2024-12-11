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
using Emoji = WeatherBot.Text.Emoji;

namespace WeatherBot;

internal class App
{
    public static SQLiteConnection Database { get; private set; } = default!;
    public static AppConfiguration Config { get; private set; } = default!;
    public static TelegramBotClient Bot { get; private set; } = default!;

    private static void Main()
    {
        new App().MainAsync().GetAwaiter().GetResult();
    }

    private async Task MainAsync()
    {
        Config = AppConfiguration.Initialize();

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
            await Bot.SetMyCommands(
                [new BotCommand { Command = "/help", Description = Translator.Get(lang, "HelpCommand") }],
                languageCode: lang
            );
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
        if (message.Location != null)
        {
            await WeatherCommands.LocationMessage(user, message);
            return;
        }

        if (string.IsNullOrEmpty(message.Text)) return;

        switch (message.Text[0])
        {
            case '/' when message.Text is "/start" or "/help":
                await BotUserCommands.HelpCommand(user, message);
                break;
            case '/' when Config.AdminsIds.Contains(user.Id):
            {
                var args = message.Text.Split(' ');
                if (args[0] == "/setquota")
                {
                    await BotUserCommands.SetQuotaCommand(message, args);
                }

                break;
            }
            case BookmarkConfiguration.Prefix:
                await WeatherCommands.BookmarkMessage(user, message);
                break;
            default:
                if (message.Text == Emoji.Cog)
                {
                    await BotUserCommands.SettingsCommand(user, message);
                }
                else
                {
                    await BookmarkCommands.SetName(user, message);
                }

                break;
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
            case "SetDefaultReportType":
                await BotUserCommands.SetDefaultReportTypeCallback(user, query, args);
                break;
            case "SetLanguage":
                await BotUserCommands.SetLanguageCallback(user, query, args);
                break;
        }
    }

    public static IReplyMarkup GetMainMenuMarkup(long userId)
    {
        var bookmarks = BookmarkEntity.List(userId);

        IEnumerable<KeyboardButton> buttons =
        [
            .. bookmarks.Select(x => new KeyboardButton(BookmarkConfiguration.Prefix + x.Name)),
            KeyboardButton.WithRequestLocation(Emoji.Pin),
            new(Emoji.Cog)
        ];

        return new ReplyKeyboardMarkup
        {
            Keyboard = buttons.Chunk(Config.Bookmarks.BookmarksPerRow),
            ResizeKeyboard = true,
            IsPersistent = true
        };
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
}