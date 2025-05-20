using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public static class MessageBuilderExtensions
{
    /// <summary>
    /// Appends the given string to this builder's content. If there is any existing content, the content is added on a new line
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
}