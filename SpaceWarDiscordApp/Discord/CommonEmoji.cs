using DSharpPlus;
using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class CommonEmoji(DiscordClient client)
{
    public DiscordEmoji ThumbsUp = DiscordEmoji.FromName(client, ":thumbsup:");
    public DiscordEmoji Heart = DiscordEmoji.FromName(client, ":heart:");
    public DiscordEmoji Laughing = DiscordEmoji.FromName(client, ":laughing:");
    public DiscordEmoji StuckOutTongue = DiscordEmoji.FromName(client, ":stuck_out_tongue:");
    public DiscordEmoji OpenMouth = DiscordEmoji.FromName(client, ":open_mouth:");
    public DiscordEmoji Cry = DiscordEmoji.FromName(client, ":cry:");
}