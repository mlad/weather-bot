namespace WeatherBot.Text;

public static class ZeroWidthCharacters
{
    public const char A = '\u200C';
    public const char B = '\u200D';
    public const char C = '\uFEFF';

    public static bool ContainsZeroWidthCharacters(this string value) =>
        value.Contains(A) || value.Contains(B) || value.Contains(C);
}