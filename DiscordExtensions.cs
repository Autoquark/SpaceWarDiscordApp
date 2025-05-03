using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace SpaceWarDiscordApp;

public static class DiscordExtensions
{
    public static async ValueTask RespondAsync(this CommandContext context, IEnumerable<DiscordMessageBuilder> messages)
    {
        foreach (var discordMessageBuilder in messages)
        {
            await context.RespondAsync(discordMessageBuilder);
        }
    }
}