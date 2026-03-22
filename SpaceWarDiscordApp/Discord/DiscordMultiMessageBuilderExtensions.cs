using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord;

public static class DiscordMultiMessageBuilderExtensions
{
    public static DiscordMultiMessageBuilder WithAllowedMentions(this DiscordMultiMessageBuilder builder, IEnumerable<GamePlayer> players)
    {
        builder.Builders.Last().AllowMentions(players);
        return builder;
    }

    public static DiscordMultiMessageBuilder WithAllowedMentions(this DiscordMultiMessageBuilder builder, GamePlayer first, params IEnumerable<GamePlayer> players)
    {
        builder.Builders.Last().AllowMentions(first, players);
        return builder;
    }

    public static DiscordMultiMessageBuilder AppendHexButtons(this DiscordMultiMessageBuilder builder, Game game, IEnumerable<BoardHex> hexes,
        IEnumerable<string> interactionIds, string? cancelId = null)
        => builder.AppendButtonRows(cancelId, hexes.Zip(interactionIds).Select(x => DiscordHelpers.CreateButtonForHex(game, x.First, x.Second)));

    public static async Task<DiscordMultiMessageBuilder> AppendPlayerButtonsAsync(this DiscordMultiMessageBuilder builder, IEnumerable<GamePlayer> players,
        IEnumerable<string> interactionIds, string? cancelId = null)
    {
        await builder.Builders.Last().AppendPlayerButtonsAsync(players, interactionIds, cancelId);
        return builder;
    }
}
