using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;

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
    
    public static TBuilder AppendButtonRows<TBuilder>(this TBuilder builder,
        params IEnumerable<DiscordButtonComponent> buttons)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        builder.AppendButtonRows(null, buttons);
        return builder;
    }

    /// <summary>
    /// Appends the given buttons to the builder, split across as many action rows as needed
    /// </summary>
    public static TBuilder AppendButtonRows<TBuilder>(this TBuilder builder,
        string? cancelId,
        params IEnumerable<DiscordButtonComponent> buttons)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        ((IDiscordMessageBuilder)builder).AppendButtonRows(cancelId, buttons);
        return builder;
    }

    public static IDiscordMessageBuilder AppendButtonRows(this IDiscordMessageBuilder builder,
        params IEnumerable<DiscordButtonComponent> buttons)
        => builder.AppendButtonRows(null, buttons);

    public static IDiscordMessageBuilder AppendButtonRows(this IDiscordMessageBuilder builder,
        string? cancelId,
        params IEnumerable<DiscordButtonComponent> buttons)
    {
        if (cancelId != null)
        {
            buttons = buttons.Append(MakeCancelButton(cancelId));
        }
        
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
        IEnumerable<BoardHex> hexes, IEnumerable<string> interactionIds, string? cancelId = null)
    {
        var buttons = hexes.Zip(interactionIds)
            .OrderBy(x => x.First.Coordinates.ToHexNumber())
            .Select(x => DiscordHelpers.CreateButtonForHex(game, x.First, x.Second));

        if (cancelId != null)
        {
            buttons = buttons.Append(MakeCancelButton(cancelId));
        }

        return builder.AppendButtonRows(buttons);
    }

    public static async Task<IDiscordMessageBuilder> AppendPlayerButtonsAsync(this IDiscordMessageBuilder builder,
        IEnumerable<GamePlayer> players, IEnumerable<string> interactionIds, string? cancelId = null)
    {
        IEnumerable<DiscordButtonComponent> buttons = await Task.WhenAll(players.Zip(interactionIds,
                async (p, i) => (Player: p, Name: await p.GetNameAsync(false, false), Id: i))
            .ToAsyncEnumerable()
            .OrderByAwait(async x => (await x).Name)
            .Select(async x =>
            {
                var (player, name, id) = await x;
                return await DiscordHelpers.CreateButtonForPlayerAsync(player, id);
            }).ToEnumerable());

        if (cancelId != null)
        {
            buttons = buttons.Append(MakeCancelButton(cancelId));
        }
        
        return builder.AppendButtonRows(buttons);
    }

    public static IDiscordMessageBuilder AppendCancelButton(this IDiscordMessageBuilder builder, string interactionId)
        => builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, interactionId, "Cancel"));

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
        builder.AllowMentions(players);
        return builder;
    }

    public static IDiscordMessageBuilder AllowMentions(this IDiscordMessageBuilder builder,
        IEnumerable<GamePlayer> players)
    {
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
    
    private static DiscordButtonComponent MakeCancelButton(string interactionId) => new DiscordButtonComponent(DiscordButtonStyle.Danger, interactionId, "Cancel");
}