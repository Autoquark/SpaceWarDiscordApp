using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp;

public static class DiscordHelpers
{
    public static DiscordButtonComponent CreateButtonForHex(Game game, BoardHex hex, string interactionId, DiscordButtonStyle style = DiscordButtonStyle.Primary)
    {
        var emoji = hex.GetDieEmoji(game);
        return new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionId,
            hex.Coordinates.ToString(), emoji: (emoji! == null! ? null : new DiscordComponentEmoji(emoji))!);
    }
}