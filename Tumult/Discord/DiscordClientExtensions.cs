using DSharpPlus;
using DSharpPlus.Entities;

namespace Tumult.Discord;

public static class DiscordClientExtensions
{
    public static async Task<DiscordUser?> TryGetUserAsync(this DiscordClient client, ulong userId)
    {
        try
        {
            return await client.GetUserAsync(userId);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
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