using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WeatherBot.Text;
using WeatherBot.Weather.Models;

namespace WeatherBot.Users;

public static class BotUserCommands
{
    public static async Task Help(BotUser user, Message message)
    {
        await App.Bot.SendMessage(
            message.Chat,
            GetWelcomeMessage(user.Language),
            replyMarkup: App.GetMainMenuMarkup(user.Id)
        );
    }

    public static async Task SetQuota(Message message, string[] args)
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

    public static async Task Lang(BotUser user, Message message, string[] args)
    {
        var target = args.ElementAtOrDefault(1);

        int index;
        if (string.IsNullOrEmpty(target))
        {
            index = Array.IndexOf(Translator.AllLanguages, user.Language) + 1;
            if (index == Translator.AllLanguages.Length)
                index = 0;
        }
        else
        {
            index = Array.IndexOf(Translator.AllLanguages, target);
            if (index == -1)
                throw new UserException($"Available languages: {string.Join(", ", Translator.AllLanguages)}");
        }

        user.Language = Translator.AllLanguages[index];
        user.Update();

        await App.Bot.SendMessage(message.Chat, GetWelcomeMessage(user.Language));
    }

    public static async Task SetWeatherType(BotUser user, Message message, WeatherReportType type)
    {
        user.WeatherType = type;
        user.Update();

        var sb = new TranslatedBuilder(user.Language);
        sb.Add("Weather:Misc:DefaultTypeChanged");
        sb.AddRaw("<b>");
        sb.Add($"FetchType:{user.WeatherType.GetKey()}:FullName");
        sb.AddRaw("</b>");

        await App.Bot.SendMessage(message.Chat, sb.ToString(), parseMode: ParseMode.Html);
    }

    private static string GetWelcomeMessage(string lang)
    {
        var sb = new TranslatedBuilder(lang);
        sb.AddLine("Welcome");

        foreach (var key in WeatherReportTypeExtensions.All.Keys)
        {
            sb.AddRaw($"/{key} - ");
            sb.AddLine($"FetchType:{key}:FullName");
        }

        foreach (var command in App.AvailableCommands)
        {
            sb.AddRaw($"{command} - ");
            sb.AddLine($"Help:{command}");
        }

        return sb.ToString();
    }
}