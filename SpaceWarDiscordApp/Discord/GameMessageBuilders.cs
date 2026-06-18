namespace SpaceWarDiscordApp.Discord;

public class GameMessageBuilders
{
    /// <summary>
    /// Builder for the channel the command was executed in.
    /// </summary>
    public required DiscordMultiMessageBuilder SourceChannelBuilder { get; set; }
    
    /// <summary>
    /// Builder for the game channel, if the command/interaction is in the context of a game.
    /// </summary>
    public DiscordMultiMessageBuilder? GameChannelBuilder { get; set; }

    /// <summary>
    /// Builders for private threads for each player.
    /// </summary>
    public Dictionary<int, DiscordMultiMessageBuilder> PlayerPrivateThreadBuilders { get; set; } = new();
}