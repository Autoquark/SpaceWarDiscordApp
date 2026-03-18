using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord;

public class GameMessageBuilders : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Builder for the channel the command was executed in.
    /// </summary>
    public DiscordMultiMessageBuilder SourceChannelBuilder { get; set; }
    
    /// <summary>
    /// Builder for the game channel, if the command/interaction is in the context of a game.
    /// </summary>
    public DiscordMultiMessageBuilder? GameChannelBuilder { get; set; }

    /// <summary>
    /// Builders for private threads for each player.
    /// </summary>
    public Dictionary<int, DiscordMultiMessageBuilder> PlayerPrivateThreadBuilders { get; set; } = new();

    public void Dispose()
    {
        GameChannelBuilder?.Dispose();
        foreach (var value in PlayerPrivateThreadBuilders.Values)
        {
            value.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (GameChannelBuilder != null)
        {
            await GameChannelBuilder.DisposeAsync();
        }

        foreach (var value in PlayerPrivateThreadBuilders.Values)
        {
            await value.DisposeAsync();
        }
    }
}