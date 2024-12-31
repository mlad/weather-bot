namespace WeatherBot.Resources;

public static class Resource
{
    public static class Font
    {
        private const string Folder = "Resources/fonts";
        public const string RobotoRegular = $"{Folder}/Roboto-Regular.ttf";
        public const string RobotoBold = $"{Folder}/Roboto-Bold.ttf";
        public const string RobotoThin = $"{Folder}/Roboto-Thin.ttf";
    }

    public static class Emoji
    {
        public const string Sun = "Resources/emoji/sun.png";
        public const string SunSmallCloud = "Resources/emoji/sun-small-cloud.png";
        public const string SunBigCloud = "Resources/emoji/sun-big-cloud.png";
        public const string Cloud = "Resources/emoji/cloud.png";
        public const string Rain = "Resources/emoji/rain.png";
        public const string Thunderstorm = "Resources/emoji/thunderstorm.png";
        public const string Fog = "Resources/emoji/fog.png";
        public const string Snow = "Resources/emoji/snow.png";
    }
}