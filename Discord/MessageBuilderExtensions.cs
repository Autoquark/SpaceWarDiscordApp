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
        => builder.AppendButtonRows(hexes.Zip(interactionIds)
            .Select(x => DiscordHelpers.CreateButtonForHex(game, x.First, x.Second)));
}