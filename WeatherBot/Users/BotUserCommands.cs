using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WeatherBot.Text;
using WeatherBot.Weather.Models;

namespace WeatherBot.Users;

public static class BotUserCommands
{
    #region Commands

    public static async Task HelpCommand(BotUser user, Message message)
    {
        await App.Bot.SendMessage(
            message.Chat,
            Translator.Get(user.Language, "Welcome"),
            replyMarkup: App.GetMainMenuMarkup(user.Id)
        );
    }

    public static async Task SettingsCommand(BotUser user, Message message)
    {
        await App.Bot.SendMessage(
            message.Chat,
            Translator.Get(user.Language, "Settings:Title"),
            replyMarkup: new InlineKeyboardMarkup()
                .AddButton(Translator.Get(user.Language, "Settings:SetDefaultReportType:Button"), "SetDefaultReportType")
                .AddButton(Translator.Get(user.Language, "Settings:SetLanguage:Button"), "SetLanguage")
        );
    }

    public static async Task SetQuotaCommand(Message message, string[] args)
    {
        var username = args.ElementAtOrDefault(1);
        var quotaStr = args.ElementAtOrDefault(2);
        if (string.IsNullOrEmpty(username) || !int.TryParse(quotaStr, out var quota))
        {
            await App.Bot.SendMessage(message.Chat, "Use: /setquota [username] [quota]");
            return;
        }

        var targetUser = BotUser.TryGetByName(username)
                         ?? throw new UserException("User not found");

        targetUser.RequestsQuota = quota;
        targetUser.Update();

        await App.Bot.SendMessage(message.Chat, $"Changed @{username} quota to {quota}");
    }

    #endregion

    #region Callbacks

    public static async Task SetDefaultReportTypeCallback(BotUser user, CallbackQuery query, string[] args)
    {
        if (args.Length != 2)
        {
            await App.Bot.EditMessageText(
                query.Message!.Chat,
                query.Message.MessageId,
                Translator.Get(user.Language, "Settings:SetDefaultReportType:Title"),
                replyMarkup: new InlineKeyboardMarkup(
                    WeatherReportTypeExtensions.ButtonOrder.Select(row => row.Select(key =>
                        new InlineKeyboardButton(Translator.Get(user.Language, $"FetchType:{key}:ShortName"))
                        {
                            CallbackData = $"SetDefaultReportType {key}"
                        })
                    )
                )
            );
        }
        else
        {
            if (!WeatherReportTypeExtensions.TryParse(args[1], out var type))
                throw new Exception($"Unknown weather report type: {args[1]}");

            user.WeatherType = type;
            user.Update();

            var sb = new TranslatedBuilder(user.Language);
            sb.Add("Settings:SetDefaultReportType:Result");
            sb.AddRaw("<b>");
            sb.Add($"FetchType:{user.WeatherType.GetKey()}:FullName");
            sb.AddRaw("</b>");

            await App.Bot.EditMessageText(query.Message!.Chat, query.Message.MessageId, sb.ToString(), ParseMode.Html);
        }
    }

    public static async Task SetLanguageCallback(BotUser user, CallbackQuery query, string[] args)
    {
        if (args.Length != 2)
        {
            await App.Bot.EditMessageText(
                query.Message!.Chat,
                query.Message.MessageId,
                Translator.Get(user.Language, "Settings:SetLanguage:Title"),
                replyMarkup: new InlineKeyboardMarkup(
                    Translator.AllLanguages.Select(x =>
                        new InlineKeyboardButton(x.ToUpper()) { CallbackData = $"SetLanguage {x}" }
                    )
                )
            );
        }
        else
        {
            if (!Translator.AllLanguages.Contains(args[1]))
                throw new Exception($"Unknown language: {args[1]}");

            user.Language = args[1];
            user.Update();

            await App.Bot.EditMessageText(
                query.Message!.Chat,
                query.Message.MessageId,
                Translator.Get(user.Language, "Settings:SetLanguage:Result")
            );
        }
    }

    #endregion
}