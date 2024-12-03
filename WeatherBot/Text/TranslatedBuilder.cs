using System.Text;

namespace WeatherBot.Text;

public class TranslatedBuilder(string lang)
{
    private readonly StringBuilder _sb = new();

    public void AddLine()
    {
        _sb.AppendLine();
    }

    public void AddLine(string key, params object?[] args)
    {
        _sb.AppendLine(Translator.Format(lang, key, args));
    }

    public void Add(string key, params object?[] args)
    {
        _sb.Append(Translator.Format(lang, key, args));
    }

    public void AddRawLine(string str)
    {
        _sb.AppendLine(str);
    }

    public void AddRaw(string str)
    {
        _sb.Append(str);
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}