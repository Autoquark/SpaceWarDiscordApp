using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SpaceWarDiscordApp.Discord;

public static class ChannelExtensions
{
    public static async Task<DiscordMessage?> TryGetMessageAsync(this DiscordChannel channel, ulong messageId)
    {
        try
        {
            return await channel.GetMessageAsync(messageId);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
}