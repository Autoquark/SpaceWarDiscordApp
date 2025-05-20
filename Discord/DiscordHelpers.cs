using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord;

public static class DiscordHelpers
{
    public static DiscordButtonComponent CreateButtonForHex(Game game, BoardHex hex, string interactionId, DiscordButtonStyle style = DiscordButtonStyle.Primary)
    {
        var emoji = hex.GetDieEmoji(game);
        return new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionId,
            hex.Coordinates.ToString(), emoji: (emoji! == null! ? null : new DiscordComponentEmoji(emoji))!);
    }

    public static string DiscordBold(this string message) => $"**{message}**";
    public static string DiscordItalic(this string message) => $"*{message}*";
    public static string DiscordStrikeThrough(this string message) => $"~~{message}~~";
    public static string DiscordHeading1(this string message) => $"# {message}";
    public static string DiscordHeading2(this string message) => $"## {message}";
    public static string DiscordHeading3(this string message) => $"### {message}";
    public static string DiscordSubtext(this string message) => $"-# {message}";
}