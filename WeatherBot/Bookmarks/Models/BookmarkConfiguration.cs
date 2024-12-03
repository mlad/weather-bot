using WeatherBot.Text;

namespace WeatherBot.Bookmarks.Models;

[Serializable]
public class BookmarkConfiguration
{
    public const char Prefix = ZeroWidthCharacters.A;

    public int MaxUserBookmarks { get; set; } = 8;
    public int BookmarksPerRow { get; set; } = 3;

    public void Validate()
    {
        if (MaxUserBookmarks < 0)
            throw new Exception($"Bookmarks: {nameof(MaxUserBookmarks)} cannot be negative");

        if (BookmarksPerRow is < 1 or > 10)
            throw new Exception($"Bookmarks: {nameof(BookmarksPerRow)} must be between 1 and 10");
    }
}