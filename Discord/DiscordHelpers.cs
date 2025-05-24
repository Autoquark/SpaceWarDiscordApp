using System.Text;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord;

public static class DiscordHelpers
{
    public static DiscordButtonComponent CreateButtonForHex(Game game, BoardHex hex, string interactionId, DiscordButtonStyle style = DiscordButtonStyle.Primary)
    {
        var emoji = hex.GetDieEmoji(game);
        return new DiscordButtonComponent(style, interactionId,
            hex.Coordinates.ToString(), emoji: (emoji! == null! ? null : new DiscordComponentEmoji(emoji))!);
    }

    public static DiscordButtonComponent CreateButtonForTechAction(TechAction techAction, string interactionId,
        DiscordButtonStyle style = DiscordButtonStyle.Primary)
    {
        return new DiscordButtonComponent(style, interactionId, techAction.DisplayName, !techAction.IsAvailable); //TODO: emojis on techs?   
    }

    public static string DiscordBold(this string message) => $"**{message}**";
    public static StringBuilder DiscordBold(this StringBuilder message) => new($"**{message}**");
    public static string DiscordItalic(this string message) => $"*{message}*";
    public static StringBuilder DiscordItalic(this StringBuilder message) => new($"*{message}*");
    public static string DiscordStrikeThrough(this string message) => $"~~{message}~~";
    public static StringBuilder DiscordStrikeThrough(this StringBuilder message) => new($"~~{message}~~");
    public static string DiscordHeading1(this string message) => $"# {message}";
    public static StringBuilder DiscordHeading1(this StringBuilder message) => new($"# {message}");
    public static string DiscordHeading2(this string message) => $"## {message}";
    public static StringBuilder DiscordHeading2(this StringBuilder message) => new($"## {message}");
    public static string DiscordHeading3(this string message) => $"### {message}";
    public static StringBuilder DiscordHeading3(this StringBuilder message) => new($"### {message}");
    public static string DiscordSubtext(this string message) => $"-# {message}";
    public static StringBuilder DiscordSubtext(this StringBuilder message) => new($"-# {message}");
}