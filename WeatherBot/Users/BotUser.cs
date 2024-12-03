using Telegram.Bot.Types;
using WeatherBot.Text;
using WeatherBot.Users.Database;
using WeatherBot.Weather.Models;

namespace WeatherBot.Users;

[Serializable]
public class BotUser(BotUserEntity entity)
{
    public long Id { get; set; } = entity.Id;
    public string? Name { get; set; } = entity.Name;
    public string Language { get; set; } = entity.Language;
    public WeatherReportType WeatherType { get; set; } = entity.DefaultWeatherType;
    public int RequestsQuota { get; set; } = entity.RequestsQuota;

    public static BotUser GetOrCreate(User telegramUser)
    {
        var entity = App.Database.Find<BotUserEntity>(telegramUser.Id);
        if (entity == null)
        {
            var language = Translator.AllLanguages.Contains(telegramUser.LanguageCode ?? string.Empty)
                ? telegramUser.LanguageCode!
                : Translator.FallbackLanguage;

            entity = new BotUserEntity
            {
                Id = telegramUser.Id,
                Name = telegramUser.Username,
                Language = language,
                DefaultWeatherType = WeatherReportType.OpenWeatherMapCurrent,
                RequestsQuota = 5
            };

            App.Database.Insert(entity);
        }

        return new BotUser(entity);
    }

    public static BotUser? TryGetByName(string username)
    {
        var entity = App.Database.Find<BotUserEntity>(x => x.Name == username);
        return entity != null ? new BotUser(entity) : null;
    }

    public void Update()
    {
        App.Database.Update(new BotUserEntity
        {
            Id = Id,
            Name = Name,
            Language = Language,
            DefaultWeatherType = WeatherType,
            RequestsQuota = RequestsQuota
        });
    }
}