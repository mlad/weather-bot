using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WeatherBot.Text;
using WeatherBot.Users;
using WeatherBot.Weather.Models;

namespace WeatherBot.Geocoding;

public static class GeocodingCommands
{
    public static async Task Search(BotUser user, Message message)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
            return;

        if (message.Text.Length > 100)
            throw new UserException("!Geocoding:NameTooLong");

        var requestsInHour = GeocodingLog.Count(user.Id, TimeSpan.FromHours(1));
        var requestsQuota = user.RequestsQuota ?? App.Config.Users.DefaultRequestsQuota;
        if (requestsInHour >= requestsQuota)
        {
            await App.Bot.SendMessage(message.Chat, user.Translate("Quota:Reached", requestsQuota));
            return;
        }

        var locations = await OpenMeteo.Query(message.Text, user.Language);

        GeocodingLog.Create(user.Id, message.Text);

        if (locations.Count == 0)
        {
            await App.Bot.SendMessage(
                message.Chat,
                user.Translate("Geocoding:NoResults"),
                replyParameters: new ReplyParameters { MessageId = message.MessageId }
            );

            return;
        }

        await App.Bot.SendMessage(
            message.Chat,
            text: string.Join('\n', locations.Select((x, i) => $"{i + 1}. {Emoji.CountryFlag(x.CountryCode)} {x.Name}")),
            replyMarkup: new InlineKeyboardMarkup(
                locations.Select((x, i) => new InlineKeyboardButton((i + 1).ToString())
                {
                    CallbackData = $"Weather {new WeatherParams(user.WeatherType, x.Latitude, x.Longitude)}"
                })
            ),
            replyParameters: new ReplyParameters { MessageId = message.MessageId }
        );
    }
}