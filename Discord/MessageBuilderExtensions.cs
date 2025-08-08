using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord;

public static class MessageBuilderExtensions
{
    /// <summary>
    /// Appends the given string to this builder. If there is any existing content, the content is added on a new line
    /// </summary>
    public static TBuilder AppendContentNewline<TBuilder>(this TBuilder builder, string content)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        ((IDiscordMessageBuilder)builder).AppendContentNewline(content);
        return builder;
    }

    public static IDiscordMessageBuilder AppendContentNewline(this IDiscordMessageBuilder builder, string content)
    {
        if (builder.Flags.HasFlag(DiscordMessageFlags.IsComponentsV2))
        {
            builder.AddTextDisplayComponent(content);
            return builder;
        }
        
        builder.Content ??= "";
        if (!string.IsNullOrEmpty(builder.Content))
        {
            builder.Content += "\n";
        }
        
        builder.Content += content;
        
        return builder;
    }

    /// <summary>
    /// Appends the given buttons to the builder, split across as many action rows as needed
    /// </summary>
    public static TBuilder AppendButtonRows<TBuilder>(this TBuilder builder,
        IEnumerable<DiscordButtonComponent> buttons)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        ((IDiscordMessageBuilder)builder).AppendButtonRows(buttons);
        return builder;
    }

    public static IDiscordMessageBuilder AppendButtonRows(this IDiscordMessageBuilder builder,
        IEnumerable<DiscordButtonComponent> buttons)
    {
        foreach (var group in buttons.ZipWithIndices().GroupBy(x => x.index / 5))
        {
            builder.AddActionRowComponent(group.Select(x => x.item));
        }
        
        return builder;
    }

    public static TBuilder AppendHexButtons<TBuilder>(this TBuilder builder,
        Game game,
        IEnumerable<BoardHex> hexes, IEnumerable<string> interactionIds)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        ((IDiscordMessageBuilder)builder).AppendHexButtons(game, hexes, interactionIds);
        return builder;
    }
    
    public static IDiscordMessageBuilder AppendHexButtons(this IDiscordMessageBuilder builder, Game game,
        IEnumerable<BoardHex> hexes, IEnumerable<string> interactionIds)
        => builder.AppendButtonRows(hexes.Zip(interactionIds)
            .Select(x => DiscordHelpers.CreateButtonForHex(game, x.First, x.Second)));

    /// <summary>
    /// Allows mentioning the given player(s) in this message (does nothing for dummy players)
    /// </summary>
    public static TBuilder AllowMentions<TBuilder>(this TBuilder builder, GamePlayer first, params IEnumerable<GamePlayer> players)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        ((IDiscordMessageBuilder)builder).AllowMentions(first, players);
        return builder;
    }

    public static IDiscordMessageBuilder AllowMentions(this IDiscordMessageBuilder builder, GamePlayer first,
        params IEnumerable<GamePlayer> players)
    {
        players = players.Append(first);
        foreach (var player in players.Where(x => !x.IsDummyPlayer))
        {
            if (builder.Mentions.OfType<UserMention>().Any(x => x.Id == player.DiscordUserId))
            {
                continue;
            }
            builder.AddMention(new UserMention(player.DiscordUserId));
        }

        return builder;
    }
}