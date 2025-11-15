using DSharpPlus;
using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public static class DiscordClientExtensions
{
    public static async Task<DiscordChannel?> TryGetChannelAsync(this DiscordClient client, ulong channelId)
    {
        try
        {
            return await client.GetChannelAsync(channelId);
        }
        catch (Exception)
        {
            return null;
        }
    }
}