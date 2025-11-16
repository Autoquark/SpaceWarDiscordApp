using System.Text;

namespace SpaceWarDiscordApp;

public static class StringExtensions
{
    public static string InsertSpacesInCamelCase(this string text) =>
        string.Concat(text.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
    
    public static string Capitalise(this string text) => string.Concat(text.First().ToString().ToUpper(), text.AsSpan(1));
    public static StringBuilder Capitalise(this StringBuilder builder)
    {
        builder[0] = char.ToUpperInvariant(builder[0]);
        return builder;
    }
}