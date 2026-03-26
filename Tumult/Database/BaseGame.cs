using Google.Cloud.Firestore;
using Tumult.Database.GameEvents;

namespace Tumult.Database;

[FirestoreData]
public abstract class BaseGame : FirestoreDocument
{
    /// <summary>
    /// The Discord channel ID associated with this game.
    /// </summary>
    [FirestoreProperty]
    public ulong GameChannelId { get; set; } = 0;

    /// <summary>
    /// Stack of events currently being resolved. The last event is on top.
    /// </summary>
    [FirestoreProperty]
    public abstract List<GameEvent> EventStack { get; set; }

    /// <summary>
    /// All players in this game.
    /// </summary>
    public abstract IReadOnlyList<BaseGamePlayer> GamePlayers { get; }

    /// <summary>
    /// Returns true if the given Discord user is a player in this game.
    /// </summary>
    public bool IsDiscordUserInGame(ulong discordUserId)
        => GamePlayers.Any(x => x.DiscordUserId == discordUserId);

    /// <summary>
    /// Returns true if the Discord user is allowed to trigger an interaction designated for the given game player.
    /// Dummy/uncontrolled players (DiscordUserId == 0) can be triggered by anyone.
    /// </summary>
    public bool IsInteractionAllowedForUser(int forGamePlayerId, ulong discordUserId)
    {
        var forPlayer = GamePlayers.FirstOrDefault(x => x.GamePlayerId == forGamePlayerId);
        if (forPlayer == null)
        {
            return false;
        }

        if (forPlayer.DiscordUserId == 0)
        {
            return true;
        }

        return GamePlayers.Any(x => x.DiscordUserId == discordUserId && x == forPlayer);
    }
}
