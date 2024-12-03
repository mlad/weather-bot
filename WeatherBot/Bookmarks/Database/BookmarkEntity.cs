using SQLite;
using WeatherBot.Text;
using WeatherBot.Weather.Models;

namespace WeatherBot.Bookmarks.Database;

[Serializable]
[Table("Bookmarks")]
public class BookmarkEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public long UserId { get; set; }
    [MaxLength(50)] public string? Name { get; set; }
    public WeatherReportType Type { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTime CreateTimeUtc { get; set; }

    public static List<BookmarkEntity> List(long userId)
    {
        return App.Database.Table<BookmarkEntity>().Where(x => x.UserId == userId && x.Name != null).ToList();
    }

    public static int Count(long userId)
    {
        return App.Database.Table<BookmarkEntity>().Count(x => x.UserId == userId);
    }

    public static BookmarkEntity? TryGet(long userId, string? name)
    {
        return App.Database.Find<BookmarkEntity>(x => x.UserId == userId && x.Name == name);
    }

    public static BookmarkEntity? TryGet(long userId, WeatherParams request)
    {
        var bookmarks = App.Database.Table<BookmarkEntity>().Where(x =>
            x.UserId == userId && x.Type == request.Type);

        return Enumerable.FirstOrDefault(bookmarks, x =>
            Math.Abs(x.Lat - request.Lat) < 0.0000001 && Math.Abs(x.Lon - request.Lon) < 0.0000001);
    }

    public static BookmarkEntity Create(long userId, WeatherParams request)
    {
        var entity = new BookmarkEntity
        {
            UserId = userId,
            Type = request.Type,
            Lat = request.Lat,
            Lon = request.Lon,
            CreateTimeUtc = DateTime.UtcNow
        };

        App.Database.Insert(entity);
        return entity;
    }

    public void UpdateName(string? name)
    {
        if (name != null)
        {
            if (name.Length is < 1 or > 50)
                throw new UserException("Bookmark:NameTooShortOrLong");

            if (name.ContainsZeroWidthCharacters())
                throw new UserException("Bookmark:NameHasInvalidCharacters");

            if (App.Database.Table<BookmarkEntity>().Any(x => x.UserId == UserId && x.Name == name))
                throw new UserException("Bookmark:NameAlreadyExists");
        }

        Name = name;
        App.Database.Update(this);
    }

    public static bool Delete(long userId, int bookmarkId)
    {
        return App.Database.Table<BookmarkEntity>().Delete(x => x.Id == bookmarkId && x.UserId == userId) > 0;
    }
}