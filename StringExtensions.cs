namespace SpaceWarDiscordApp;

public static class StringExtensions
{
    public static string InsertSpacesInCamelCase(this string text) =>
        string.Concat(text.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
}