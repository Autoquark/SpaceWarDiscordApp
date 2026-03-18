using System.Text;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord;

public static partial class DiscordHelpers
{
    private static Regex emojiReplacementRegex = MyRegex();
    
    public static DiscordButtonComponent CreateButtonForHex(Game game, BoardHex hex, string interactionId, DiscordButtonStyle style = DiscordButtonStyle.Primary)
    {
        var emoji = hex.GetDieEmoji(game);
        return new DiscordButtonComponent(style, interactionId,
            hex.Coordinates.ToHexNumberString(), emoji: (emoji! == null! ? null : new DiscordComponentEmoji(emoji))!);
    }

    public static DiscordButtonComponent CreateButtonForTechAction(TechAction techAction, string interactionId,
        DiscordButtonStyle style = DiscordButtonStyle.Primary)
    {
        return new DiscordButtonComponent(style, interactionId, techAction.DisplayName, !techAction.IsAvailable); //TODO: emojis on techs?   
    }

    public static async Task<DiscordButtonComponent> CreateButtonForPlayerAsync(GamePlayer player, string interactionId,
        DiscordButtonStyle style = DiscordButtonStyle.Primary)
    {
        return new DiscordButtonComponent(style, interactionId, await player.GetNameAsync(false, false),
            emoji: new DiscordComponentEmoji(player.PlayerColourInfo.BlankDieEmoji));
    }
    
    public static DiscordButtonComponent CreateCancelButton(string interactionId) => new(DiscordButtonStyle.Danger, interactionId, "Cancel");
    public static DiscordButtonComponent CreateShowBoardButton(string interactionId) => new(DiscordButtonStyle.Secondary, interactionId, "Show Board");

    // Replaces icon tokens with the appropriate discord emoji
    public static string ReplaceIconTokens(this string text)
    {
        StringBuilder stringBuilder = new(text);
        foreach (Match match in emojiReplacementRegex.Matches(text))
        {
            stringBuilder.Replace(match.Value, Program.AppEmojisByName[match.Value.Substring(1, match.Value.Length - 2)].ToString());
        }
        return stringBuilder.ToString();
    }

    public static string FormatToDiscordMarkdown(IEnumerable<FormattedTextRun> formattedTextRuns)
    {
        var text = new StringBuilder();
        foreach (var formattedTextRun in formattedTextRuns)
        {
            text.Append(FormatToDiscordMarkdown(formattedTextRun));
        }

        return text.ToString();
    }
    
    public static StringBuilder FormatToDiscordMarkdown(FormattedTextRun formattedTextRun)
    {
        var text = new StringBuilder(formattedTextRun.Text);
        if (formattedTextRun.IsBold)
        {
            text = text.DiscordBold();
        }

        if (formattedTextRun.IsItalic)
        {
            text = text.DiscordItalic();
        }

        if (formattedTextRun.IsStrikethrough)
        {
            text = text.DiscordStrikeThrough();
        }

        return text;
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
    
    [GeneratedRegex(@"\$[^$]+\$")]
    private static partial Regex MyRegex();
}