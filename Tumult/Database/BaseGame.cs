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
    /// Returns true if the given Discord user is a player in this game.
    /// </summary>
    public abstract bool IsDiscordUserInGame(ulong discordUserId);

    /// <summary>
    /// Returns true if the Discord user is allowed to trigger an interaction designated for the given game player.
    /// Implementations should allow triggering on behalf of dummy/uncontrolled players.
    /// </summary>
    public abstract bool IsInteractionAllowedForUser(int forGamePlayerId, ulong discordUserId);
}
